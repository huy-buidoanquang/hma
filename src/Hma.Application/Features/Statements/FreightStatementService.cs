using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Persistence;
using Hma.Application.Common.Authorization;
using Hma.Application.Abstractions.Persistence;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Statements;

public class FreightStatementService(
    IHmaDbContext db,
    IDocumentNumberService numbers,
    ICurrentUser current,
    IChangeLogService log,
    TimeProvider timeProvider)
{
    public async Task<List<FreightStatementDetails>> ListDetailsAsync(int? customerId, CancellationToken ct = default) =>
        (await ListAsync(customerId, ct)).Select(ToDetails).ToList();

    public async Task<FreightStatementDetails?> GetDetailsAsync(int id, CancellationToken ct = default)
    {
        var statement = await GetAsync(id, ct);
        return statement is null ? null : ToDetails(statement);
    }

    public async Task<FreightStatementDetails> GenerateDetailsAsync(
        int customerId, int year, int month, CancellationToken ct = default) =>
        ToDetails(await GenerateAsync(customerId, year, month, ct));

    public async Task<FreightStatementDetails> SubmitDetailsAsync(int id, CancellationToken ct = default) =>
        ToDetails(await SubmitAsync(id, ct));

    public async Task<FreightStatementDetails> FinalizeDetailsAsync(int id, CancellationToken ct = default) =>
        ToDetails(await FinalizeAsync(id, ct));

    public async Task<FreightStatementDetails> VoidDetailsAsync(
        int id, string reason, CancellationToken ct = default) =>
        ToDetails(await VoidAsync(id, reason, ct));

    private Task<List<FreightStatement>> ListAsync(int? customerId, CancellationToken ct = default)
    {
        var q = db.FreightStatements.AsNoTracking().Include(s => s.Customer).AsQueryable();
        if (customerId is not null) q = q.Where(s => s.CustomerId == customerId);
        return q.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).Take(200).ToListAsync(ct);
    }

    private Task<FreightStatement?> GetAsync(int id, CancellationToken ct = default) =>
        db.FreightStatements.Include(s => s.Customer).Include(s => s.Lines)
            .Include(s => s.CreatedByUser)
            .Include(s => s.SubmittedByUser)
            .Include(s => s.FinalizedByUser)
            .Include(s => s.VoidedByUser)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    private Task<FreightStatement> GenerateAsync(int customerId, int year, int month, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => GenerateCoreAsync(customerId, year, month, token), ct);

    private async Task<FreightStatement> GenerateCoreAsync(
        int customerId,
        int year,
        int month,
        CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.Statements, PermissionAction.Create);
        var vatParam = await db.Parameters.FirstOrDefaultAsync(p => p.Key == "VatRate", ct);
        var vatRate = 10m;
        if (vatParam?.Value is not null && decimal.TryParse(vatParam.Value, out var parsed)) vatRate = parsed;

        var orders = await db.DispatchOrders
            .AsNoTracking()
            .Include(d => d.Vehicle)
            .Include(d => d.Driver)
            .Include(d => d.VehicleType)
            .Include(d => d.Stops)
            .Include(d => d.Route)
            .Include(d => d.Documents)
            .Where(d => d.CustomerId == customerId
                        && d.BillingYear == year && d.BillingMonth == month
                        && d.Status == DispatchStatus.Completed
                        && d.ReconciliationStatus == ReconciliationStatus.Reconciled
                        && d.Documents.Any(x => x.Kind == DispatchDocumentKind.DeliveryNote))
            .OrderBy(d => d.PickupAt)
            .ToListAsync(ct);

        var existing = await db.FreightStatements
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.Year == year && s.Month == month, ct);
        FreightStatementRules.EnsureCanGenerate(existing);

        FreightStatement statement;
        if (existing is not null)
        {
            foreach (var line in existing.Lines.ToList()) db.Remove(line);
            statement = existing;
        }
        else
        {
            statement = new FreightStatement
            {
                Code = await numbers.NextAsync("freight-statement", ct),
                CustomerId = customerId,
                Year = year,
                Month = month,
                CreatedAt = timeProvider.GetLocalNow().DateTime,
                CreatedByUserId = current.UserId,
                Status = FinancialDocumentStatus.Draft
            };
            db.Add(statement);
        }

        statement.VatRate = vatRate;
        statement.Lines.Clear();
        foreach (var o in orders)
        {
            DispatchWorkflowRules.EnsureCanIncludeInStatement(o);
            statement.Lines.Add(new FreightStatementLine
            {
                DispatchOrderId = o.Id,
                TripDate = o.PickupAt,
                DispatchCode = o.Code,
                Route = o.RouteLabel,
                PlateNumber = o.Vehicle?.PlateNumber,
                Tonnage = o.VehicleType?.Name,
                DriverName = o.Driver?.Name,
                UnitPrice = o.UnitPrice,
                Surcharge = o.Surcharge,
                ExtraCost = o.BillableExtraCost,
                LineTotal = o.TotalAmount,
                Notes = o.Notes
            });
        }

        statement.TripCount = statement.Lines.Count;
        statement.FreightTotal = statement.Lines.Sum(l => l.UnitPrice);
        statement.SurchargeTotal = statement.Lines.Sum(l => l.Surcharge);
        statement.ExtraCostTotal = statement.Lines.Sum(l => l.ExtraCost);
        statement.GrandTotal = statement.Lines.Sum(l => l.LineTotal);
        statement.VatAmount = Math.Round(statement.GrandTotal * vatRate / 100m, 2);
        statement.TotalWithVat = statement.GrandTotal + statement.VatAmount;
        await ConcurrencyConflict.SaveAsync(db, ct);
        return await GetAsync(statement.Id, ct) ?? statement;
    }

    private Task<FreightStatement> SubmitAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => SubmitCoreAsync(id, token), ct);

    private async Task<FreightStatement> SubmitCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.Statements, PermissionAction.Update);
        var statement = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy bảng kê.");
        FreightStatementRules.EnsureCanSubmit(statement, current.UserId);
        statement.Status = FinancialDocumentStatus.Submitted;
        statement.SubmittedAt = timeProvider.GetLocalNow().DateTime;
        statement.SubmittedByUserId = current.UserId;
        db.Update(statement);
        db.ApplyOriginalRowVersion(statement, statement.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("FreightStatement", id, "Submit", "Gửi duyệt bảng kê", null, null, ct);
        return await GetAsync(id, ct) ?? statement;
    }

    private Task<FreightStatement> FinalizeAsync(int id, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => FinalizeCoreAsync(id, token), ct);

    private async Task<FreightStatement> FinalizeCoreAsync(int id, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.Statements, PermissionAction.Update);
        var statement = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy bảng kê.");
        FreightStatementRules.EnsureCanFinalize(statement, current.UserId);
        statement.Status = FinancialDocumentStatus.Finalized;
        statement.FinalizedAt = timeProvider.GetLocalNow().DateTime;
        statement.FinalizedByUserId = current.UserId;
        db.Update(statement);
        db.ApplyOriginalRowVersion(statement, statement.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("FreightStatement", id, "Finalize", "Chốt bảng kê", null, null, ct);
        return await GetAsync(id, ct) ?? statement;
    }

    private Task<FreightStatement> VoidAsync(int id, string reason, CancellationToken ct = default) =>
        db.ExecuteInTransactionAsync(token => VoidCoreAsync(id, reason, token), ct);

    private async Task<FreightStatement> VoidCoreAsync(int id, string reason, CancellationToken ct)
    {
        PermissionGuard.Require(current, ScreenKeys.Statements, PermissionAction.Update);
        var statement = await GetAsync(id, ct) ?? throw new InvalidOperationException("Không tìm thấy bảng kê.");
        FreightStatementRules.EnsureCanVoid(statement, current.IsManager, reason);
        statement.Status = FinancialDocumentStatus.Voided;
        statement.VoidedAt = timeProvider.GetLocalNow().DateTime;
        statement.VoidedByUserId = current.UserId;
        statement.VoidReason = reason.Trim();
        db.Update(statement);
        db.ApplyOriginalRowVersion(statement, statement.RowVersion);
        await ConcurrencyConflict.SaveAsync(db, ct);
        await log.RecordAsync("FreightStatement", id, "Void", "Hủy bảng kê", null,
            new { Reason = statement.VoidReason }, ct);
        return await GetAsync(id, ct) ?? statement;
    }
    private static FreightStatementDetails ToDetails(FreightStatement statement) => new(
        statement.Id,
        statement.Code,
        statement.CustomerId,
        statement.Customer is null ? null : new Hma.Application.Features.Customers.CustomerOption(
            statement.Customer.Id, statement.Customer.Code, statement.Customer.Name, statement.Customer.Address,
            statement.Customer.Phone, statement.Customer.TaxCode, statement.Customer.IsWalkIn),
        statement.Year,
        statement.Month,
        statement.Status,
        statement.SubmittedByUserId,
        statement.VoidReason,
        statement.TripCount,
        statement.FreightTotal,
        statement.SurchargeTotal,
        statement.ExtraCostTotal,
        statement.GrandTotal,
        statement.VatRate,
        statement.VatAmount,
        statement.TotalWithVat,
        statement.Notes,
        statement.Lines.OrderBy(x => x.TripDate).Select(line => new FreightStatementLineSummary(
            line.Id, line.TripDate, line.DispatchCode, line.Route, line.PlateNumber, line.Tonnage,
            line.DriverName, line.UnitPrice, line.Surcharge, line.ExtraCost, line.LineTotal, line.Notes)).ToList());
}
