using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class FreightStatementService(IHmaDbContext db, IDocumentNumberService numbers, ICurrentUser current)
{
    public Task<List<FreightStatement>> ListAsync(int? customerId, CancellationToken ct = default)
    {
        var q = db.FreightStatements.AsNoTracking().Include(s => s.Customer).AsQueryable();
        if (customerId is not null) q = q.Where(s => s.CustomerId == customerId);
        return q.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).Take(200).ToListAsync(ct);
    }

    public Task<FreightStatement?> GetAsync(int id, CancellationToken ct = default) =>
        db.FreightStatements.Include(s => s.Customer).Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<FreightStatement> GenerateAsync(int customerId, int year, int month, CancellationToken ct = default)
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
            .Where(d => d.CustomerId == customerId
                        && d.BillingYear == year && d.BillingMonth == month
                        && (d.ReconciliationStatus == ReconciliationStatus.Reconciled
                            || d.Status == DispatchStatus.Locked))
            .OrderBy(d => d.PickupAt)
            .ToListAsync(ct);

        var existing = await db.FreightStatements
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.Year == year && s.Month == month, ct);

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
                CreatedAt = DateTime.Now
            };
            db.Add(statement);
        }

        statement.VatRate = vatRate;
        statement.Lines.Clear();
        foreach (var o in orders)
        {
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
                ExtraCost = o.ExtraCost,
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
}
