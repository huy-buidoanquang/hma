using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class DashboardQueryService(IHmaDbContext db)
{
    public async Task<DashboardSnapshot> MonthAsync(int year, int month, CancellationToken ct = default)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var orders = await db.DispatchOrders.AsNoTracking().Include(d => d.Documents)
            .Where(d => d.PickupAt >= start && d.PickupAt < end)
            .ToListAsync(ct);

        return new DashboardSnapshot
        {
            Year = year,
            Month = month,
            FreightTotal = orders.Sum(o => o.TotalAmount),
            PendingReconcile = orders.Count(o => o.ReconciliationStatus == ReconciliationStatus.Pending),
            Reconciled = orders.Count(o => o.ReconciliationStatus == ReconciliationStatus.Reconciled),
            WaitingDocuments = orders.Count(o => !o.Documents.Any(d => d.Kind == DispatchDocumentKind.DeliveryNote)),
            TripCount = orders.Count
        };
    }

    public async Task<List<CustomerReportRow>> ByCustomerAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var orders = await db.DispatchOrders.AsNoTracking().Include(d => d.Customer)
            .Where(d => d.PickupAt >= from && d.PickupAt <= to)
            .ToListAsync(ct);
        return orders.GroupBy(o => o.Customer?.Name ?? "(không tên)")
            .Select(g => new CustomerReportRow(g.Key, g.Count(), g.Sum(x => x.TotalAmount)))
            .OrderByDescending(r => r.Freight)
            .ToList();
    }

    public async Task<List<VehicleReportRow>> ByVehicleAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var orders = await db.DispatchOrders.AsNoTracking().Include(d => d.Vehicle).Include(d => d.Stops)
            .Where(d => d.PickupAt >= from && d.PickupAt <= to)
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
