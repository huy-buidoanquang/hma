using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Persistence;
using Hma.Application.Common.Authorization;
using Hma.Application.Abstractions.Persistence;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.PartnerSettlements;

public class PartnerSettlementService(
    IHmaDbContext db,
    IDocumentNumberService numbers,
    ICurrentUser current,
    IChangeLogService log,
    TimeProvider timeProvider)
{
    public async Task<List<PartnerSettlementDetails>> ListDetailsAsync(CancellationToken ct = default) =>
        (await ListAsync(ct)).Select(ToDetails).ToList();

    public async Task<PartnerSettlementDetails?> GetDetailsAsync(int id, CancellationToken ct = default)
    {
        var settlement = await GetAsync(id, ct);
        return settlement is null ? null : ToDetails(settlement);
    }

    public async Task<PartnerSettlementDetails> GenerateDetailsAsync(
        int partnerId, int year, int month, CancellationToken ct = default) =>
        ToDetails(await GenerateAsync(partnerId, year, month, ct));

    public async Task<PartnerSettlementDetails> SubmitDetailsAsync(int id, CancellationToken ct = default) =>
        ToDetails(await SubmitAsync(id, ct));

    public async Task<PartnerSettlementDetails> FinalizeDetailsAsync(int id, CancellationToken ct = default) =>
        ToDetails(await FinalizeAsync(id, ct));

    public async Task<PartnerSettlementDetails> VoidDetailsAsync(
        int id, string reason, CancellationToken ct = default) =>
        ToDetails(await VoidAsync(id, reason, ct));

    private Task<List<PartnerSettlement>> ListAsync(CancellationToken ct = default) =>
        db.PartnerSettlements.AsNoTracking()
            .Include(x => x.Partner)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .Take(200)
            .ToListAsync(ct);

    private Task<PartnerSettlement?> GetAsync(int id, CancellationToken ct = default) =>
        db.PartnerSettlements
            .Include(x => x.Partner)
            .Include(x => x.Lines)
            .Include(x => x.SubmittedByUser)
            .Include(x => x.FinalizedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    private Task<PartnerSettlement> GenerateAsync(
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
            CreatedAt = timeProvider.GetLocalNow().DateTime,
            CreatedByUserId = current.UserId
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

    private Task<PartnerSettlement> SubmitAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => SubmitCoreAsync(id, token), ct);

    private async Task<PartnerSettlement> SubmitCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.PartnerSettlements, PermissionAction.Update);
        var settlement = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy quyết toán đối tác.");
        PartnerSettlementRules.EnsureCanSubmit(settlement, current.UserId);
        settlement.Status = FinancialDocumentStatus.Submitted;
        settlement.SubmittedAt = timeProvider.GetLocalNow().DateTime;
        settlement.SubmittedByUserId = current.UserId;
        db.Update(settlement);
        db.ApplyOriginalRowVersion(settlement, settlement.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("PartnerSettlement", id, "Submit", "Gửi duyệt quyết toán đối tác", null, null, ct);
        return await GetAsync(id, ct) ?? settlement;
    }

    private Task<PartnerSettlement> FinalizeAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => FinalizeCoreAsync(id, token), ct);

    private async Task<PartnerSettlement> FinalizeCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.PartnerSettlements, PermissionAction.Update);
        var settlement = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy quyết toán đối tác.");
        PartnerSettlementRules.EnsureCanFinalize(settlement, current.UserId);
        settlement.Status = FinancialDocumentStatus.Finalized;
        settlement.FinalizedAt = timeProvider.GetLocalNow().DateTime;
        settlement.FinalizedByUserId = current.UserId;
        db.Update(settlement);
        db.ApplyOriginalRowVersion(settlement, settlement.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("PartnerSettlement", id, "Finalize", "Chốt quyết toán đối tác", null, null, ct);
        return await GetAsync(id, ct) ?? settlement;
    }

    private Task<PartnerSettlement> VoidAsync(int id, string reason, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => VoidCoreAsync(id, reason, token), ct);

    private async Task<PartnerSettlement> VoidCoreAsync(int id, string reason, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.PartnerSettlements, PermissionAction.Update);
        var settlement = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy quyết toán đối tác.");
        PartnerSettlementRules.EnsureCanVoid(settlement, current.IsManager, reason);
        settlement.Status = FinancialDocumentStatus.Voided;
        settlement.VoidedAt = timeProvider.GetLocalNow().DateTime;
        settlement.VoidedByUserId = current.UserId;
        settlement.VoidReason = reason.Trim();
        db.Update(settlement);
        db.ApplyOriginalRowVersion(settlement, settlement.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("PartnerSettlement", id, "Void", "Hủy quyết toán đối tác", null,
            new { Reason = settlement.VoidReason }, ct);
        return await GetAsync(id, ct) ?? settlement;
    }
    private static PartnerSettlementDetails ToDetails(PartnerSettlement settlement) => new(
        settlement.Id,
        settlement.Code,
        settlement.PartnerId,
        settlement.Partner is null ? null : new Hma.Application.Features.Catalogs.PartnerOption(
            settlement.Partner.Id, settlement.Partner.Code, settlement.Partner.Name,
            settlement.Partner.OperatingFeePercent),
        settlement.Year,
        settlement.Month,
        settlement.Status,
        settlement.SubmittedByUserId,
        settlement.VoidReason,
        settlement.TripCount,
        settlement.GrossAmount,
        settlement.OperatingFeeAmount,
        settlement.PayableAmount,
        settlement.Notes,
        settlement.Lines.OrderBy(x => x.TripDate).Select(line => new PartnerSettlementLineSummary(
            line.Id, line.TripDate, line.DispatchCode, line.Route, line.PlateNumber, line.DriverName,
            line.BuyTotal, line.OperatingFeePercent, line.OperatingFeeAmount, line.PayableAmount)).ToList());
}
