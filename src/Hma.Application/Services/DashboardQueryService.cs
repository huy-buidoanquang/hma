using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class DashboardQueryService(IHmaDbContext db)
{
    public async Task<DashboardSnapshot> MonthAsync(int year, int month, CancellationToken ct = default)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var orders = await db.DispatchOrders.AsNoTracking()
            .Include(d => d.Documents)
            .Include(d => d.TransportExceptions)
            .Where(d => d.PickupAt >= start && d.PickupAt < end)
            .ToListAsync(ct);

        return new DashboardSnapshot
        {
            Year = year,
            Month = month,
            FreightTotal = DispatchKpiRules.EarnedRevenue(orders),
            PartnerPayableTotal = DispatchKpiRules.EarnedPartnerPayable(orders),
            GrossMarginTotal = DispatchKpiRules.EarnedGrossMargin(orders),
            PendingExceptions = DispatchKpiRules.PendingExceptionCount(orders),
            PendingReconcile = orders.Count(o => DispatchKpiRules.IsEarnedTrip(o)
                && o.ReconciliationStatus != ReconciliationStatus.Reconciled),
            Reconciled = orders.Count(o => DispatchKpiRules.IsEarnedTrip(o)
                && o.ReconciliationStatus == ReconciliationStatus.Reconciled),
            WaitingDocuments = orders.Count(o => DispatchKpiRules.IsEarnedTrip(o)
                && !o.Documents.Any(d => d.Kind == DispatchDocumentKind.DeliveryNote)),
            TripCount = orders.Count(DispatchKpiRules.IsEarnedTrip),
            DraftCount = orders.Count(o => o.Status == DispatchStatus.Draft),
            IssuedCount = orders.Count(o => o.Status == DispatchStatus.Issued),
            CompletedCount = orders.Count(o => o.Status == DispatchStatus.Completed),
            CancelledCount = orders.Count(o => o.Status == DispatchStatus.Cancelled)
        };
    }

    public async Task<List<CustomerReportRow>> ByCustomerAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var period = ReportingPeriod.InclusiveDays(from, to);
        var orders = await db.DispatchOrders.AsNoTracking().Include(d => d.Customer)
            .Where(d => d.PickupAt >= period.StartInclusive
                        && d.PickupAt < period.EndExclusive
                        && d.Status == DispatchStatus.Completed)
            .ToListAsync(ct);
        return orders.GroupBy(o => o.Customer?.Name ?? "(không tên)")
            .Select(g => new CustomerReportRow(g.Key, g.Count(), g.Sum(x => x.TotalAmount)))
            .OrderByDescending(r => r.Freight)
            .ToList();
    }

    public async Task<List<VehicleReportRow>> ByVehicleAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var period = ReportingPeriod.InclusiveDays(from, to);
        var orders = await db.DispatchOrders.AsNoTracking().Include(d => d.Vehicle).Include(d => d.Stops)
            .Where(d => d.PickupAt >= period.StartInclusive
                        && d.PickupAt < period.EndExclusive
                        && d.Status == DispatchStatus.Completed)
            .ToListAsync(ct);
        return orders.GroupBy(o => o.Vehicle?.PlateNumber ?? "(chưa gán)")
            .Select(g => new VehicleReportRow(
                g.Key,
                g.Count(),
                g.Sum(x => x.TotalAmount),
                string.Join(", ", g.Select(x => x.DeliveryLocationName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct())))
            .OrderByDescending(r => r.Freight)
            .ToList();
    }
}
