using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class ReportQueryService(IHmaDbContext db)
{
    public Task<List<DispatchOrder>> DailyDispatchAsync(DateTime day, CancellationToken ct = default) =>
        db.DispatchOrders.AsNoTracking()
            .Include(d => d.Customer)
            .Include(d => d.Driver)
            .Include(d => d.Vehicle)
            .Include(d => d.Stops)
            .Include(d => d.Route)
            .Where(d => d.PickupAt.Date == day.Date)
            .OrderBy(d => d.Code)
            .ToListAsync(ct);

    public Task<List<DispatchOrder>> PeriodDispatchAsync(DateTime from, DateTime to, int? customerId = null, CancellationToken ct = default)
    {
        var period = ReportingPeriod.InclusiveDays(from, to);
        var q = db.DispatchOrders.AsNoTracking()
            .Include(d => d.Customer)
            .Include(d => d.Driver)
            .Include(d => d.Vehicle).ThenInclude(v => v!.Partner)
            .Include(d => d.Stops)
            .Include(d => d.Route)
            .Include(d => d.PaymentMethod)
            .Where(d => d.PickupAt >= period.StartInclusive && d.PickupAt < period.EndExclusive);
        if (customerId is not null) q = q.Where(d => d.CustomerId == customerId);
        return q.OrderBy(d => d.PickupAt).ThenBy(d => d.Code).ToListAsync(ct);
    }

    public Task<List<CashPayment>> DailyPaymentsAsync(DateTime day, CancellationToken ct = default) =>
        db.CashPayments.AsNoTracking().Include(p => p.Customer).Include(p => p.DriverEmployee)
            .Where(p => p.DocumentDate.Date == day.Date)
            .OrderBy(p => p.Code)
            .ToListAsync(ct);
}
