using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class PartnerSettlementService(
    IHmaDbContext db,
    IDocumentNumberService numbers,
    ICurrentUser current,
    IChangeLogService log)
{
    public Task<List<PartnerSettlement>> ListAsync(CancellationToken ct = default) =>
        db.PartnerSettlements.AsNoTracking()
            .Include(x => x.Partner)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .Take(200)
            .ToListAsync(ct);

    public Task<PartnerSettlement?> GetAsync(int id, CancellationToken ct = default) =>
        db.PartnerSettlements
            .Include(x => x.Partner)
            .Include(x => x.Lines)
            .Include(x => x.SubmittedByUser)
            .Include(x => x.FinalizedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<PartnerSettlement> GenerateAsync(
        int partnerId,
        int year,
        int month,
        CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => GenerateCoreAsync(partnerId, year, month, token), ct);

    private async Task<PartnerSettlement> GenerateCoreAsync(
        int partnerId,
        int year,
        int month,
        CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.PartnerSettlements, PermissionAction.Create);
        if (month is < 1 or > 12 || year < 2000)
            throw new InvalidOperationException("Kỳ quyết toán không hợp lệ.");
        if (!await db.Partners.AnyAsync(x => x.Id == partnerId, ct))
            throw new InvalidOperationException("Đối tác không tồn tại.");

        var existing = await db.PartnerSettlements.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.PartnerId == partnerId && x.Year == year && x.Month == month, ct);
        PartnerSettlementRules.EnsureCanGenerate(existing);
        var existingId = existing?.Id ?? 0;
        var orders = await db.DispatchOrders.AsNoTracking()
            .Include(x => x.Documents)
            .Include(x => x.TransportExceptions)
            .Include(x => x.Route)
            .Include(x => x.Stops)
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .Where(x => x.PartnerId == partnerId
                        && x.BillingYear == year
                        && x.BillingMonth == month
                        && x.Status == DispatchStatus.Completed
                        && x.Documents.Any(d => d.Kind == DispatchDocumentKind.DeliveryNote)
                        && !db.PartnerSettlementLines.Any(line =>
                            line.DispatchOrderId == x.Id && line.PartnerSettlementId != existingId))
            .OrderBy(x => x.PickupAt)
            .ToListAsync(ct);

        var settlement = existing ?? new PartnerSettlement
        {
            Code = await numbers.NextAsync("partner-settlement", ct),
            PartnerId = partnerId,
            Year = year,
            Month = month,
            CreatedAt = DateTime.Now,
            CreatedByUserId = current.User?.Id
        };
        if (existing is null)
            db.Add(settlement);
        else
            foreach (var line in existing.Lines.ToList()) db.Remove(line);

        settlement.Lines.Clear();
        foreach (var order in orders)
        {
            PartnerSettlementRules.EnsureOrderEligible(order, partnerId);
            var fee = order.BuyTotal - order.PartnerPayableAmount;
            settlement.Lines.Add(new PartnerSettlementLine
            {
                DispatchOrderId = order.Id,
                TripDate = order.PickupAt,
                DispatchCode = order.Code,
                Route = order.RouteLabel,
                PlateNumber = order.Vehicle?.PlateNumber,
                DriverName = order.Driver?.Name,
                BuyTotal = order.BuyTotal,
                OperatingFeePercent = order.PartnerOperatingFeePercent,
                OperatingFeeAmount = fee,
                PayableAmount = order.PartnerPayableAmount
            });
        }

        settlement.TripCount = settlement.Lines.Count;
        settlement.GrossAmount = settlement.Lines.Sum(x => x.BuyTotal);
        settlement.OperatingFeeAmount = settlement.Lines.Sum(x => x.OperatingFeeAmount);
        settlement.PayableAmount = settlement.Lines.Sum(x => x.PayableAmount);
        await ConcurrencyConflict.SaveAsync(db, ct);
        return await GetAsync(settlement.Id, ct) ?? settlement;
    }

    public Task<PartnerSettlement> SubmitAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => SubmitCoreAsync(id, token), ct);

    private async Task<PartnerSettlement> SubmitCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.PartnerSettlements, PermissionAction.Update);
        var settlement = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy quyết toán đối tác.");
        PartnerSettlementRules.EnsureCanSubmit(settlement, current.User?.Id);
        settlement.Status = FinancialDocumentStatus.Submitted;
        settlement.SubmittedAt = DateTime.Now;
        settlement.SubmittedByUserId = current.User?.Id;
        db.Update(settlement);
        db.ApplyOriginalRowVersion(settlement, settlement.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("PartnerSettlement", id, "Submit", "Gửi duyệt quyết toán đối tác", null, null, ct);
        return await GetAsync(id, ct) ?? settlement;
    }

    public Task<PartnerSettlement> FinalizeAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => FinalizeCoreAsync(id, token), ct);

    private async Task<PartnerSettlement> FinalizeCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.PartnerSettlements, PermissionAction.Update);
        var settlement = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy quyết toán đối tác.");
        PartnerSettlementRules.EnsureCanFinalize(settlement, current.User?.Id);
        settlement.Status = FinancialDocumentStatus.Finalized;
        settlement.FinalizedAt = DateTime.Now;
        settlement.FinalizedByUserId = current.User?.Id;
        db.Update(settlement);
        db.ApplyOriginalRowVersion(settlement, settlement.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("PartnerSettlement", id, "Finalize", "Chốt quyết toán đối tác", null, null, ct);
        return await GetAsync(id, ct) ?? settlement;
    }

    public Task<PartnerSettlement> VoidAsync(int id, string reason, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => VoidCoreAsync(id, reason, token), ct);

    private async Task<PartnerSettlement> VoidCoreAsync(int id, string reason, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.PartnerSettlements, PermissionAction.Update);
        var settlement = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy quyết toán đối tác.");
        PartnerSettlementRules.EnsureCanVoid(settlement, current.User?.IsManager == true, reason);
        settlement.Status = FinancialDocumentStatus.Voided;
        settlement.VoidedAt = DateTime.Now;
        settlement.VoidedByUserId = current.User?.Id;
        settlement.VoidReason = reason.Trim();
        db.Update(settlement);
        db.ApplyOriginalRowVersion(settlement, settlement.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("PartnerSettlement", id, "Void", "Hủy quyết toán đối tác", null,
            new { Reason = settlement.VoidReason }, ct);
        return await GetAsync(id, ct) ?? settlement;
    }
}
