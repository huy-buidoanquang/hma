using Hma.Application.Abstractions.Persistence;
using Hma.Application.Features.Accounting;
using Hma.Application.Features.Dispatching;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Reporting;

public class ReportQueryService(IHmaDbContext db)
{
    public async Task<List<DispatchOrderSummary>> DailyDispatchAsync(DateTime day, CancellationToken ct = default)
    {
        var rows = await db.DispatchOrders.AsNoTracking()
            .Include(d => d.Customer)
            .Include(d => d.Driver)
            .Include(d => d.Vehicle).ThenInclude(v => v!.Partner)
            .Include(d => d.VehicleType)
            .Include(d => d.PaymentMethod)
            .Include(d => d.Stops)
            .Include(d => d.Route)
            .Where(d => d.PickupAt.Date == day.Date)
            .OrderBy(d => d.Code)
            .ToListAsync(ct);
        return rows.Select(DispatchOrderQueryService.ToSummary).ToList();
    }

    public async Task<List<DispatchOrderSummary>> PeriodDispatchAsync(DateTime from, DateTime to, int? customerId = null, CancellationToken ct = default)
    {
        var period = ReportingPeriod.InclusiveDays(from, to);
        var q = db.DispatchOrders.AsNoTracking()
            .Include(d => d.Customer)
            .Include(d => d.Driver)
            .Include(d => d.Vehicle).ThenInclude(v => v!.Partner)
            .Include(d => d.VehicleType)
            .Include(d => d.Stops)
            .Include(d => d.Route)
            .Include(d => d.PaymentMethod)
            .Where(d => d.PickupAt >= period.StartInclusive && d.PickupAt < period.EndExclusive);
        if (customerId is not null) q = q.Where(d => d.CustomerId == customerId);
        var rows = await q.OrderBy(d => d.PickupAt).ThenBy(d => d.Code).ToListAsync(ct);
        return rows.Select(DispatchOrderQueryService.ToSummary).ToList();
    }

    public async Task<List<CashPaymentDetails>> DailyPaymentsAsync(DateTime day, CancellationToken ct = default)
    {
        var rows = await db.CashPayments.AsNoTracking().Include(p => p.Customer).Include(p => p.DriverEmployee)
            .Where(p => p.DocumentDate.Date == day.Date)
            .OrderBy(p => p.Code)
            .ToListAsync(ct);
        return rows.Select(CashDocumentService.ToDetails).ToList();
    }
}
