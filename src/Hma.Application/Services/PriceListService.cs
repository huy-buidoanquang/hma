using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class PriceListService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<PriceList>> ListAsync(CancellationToken ct = default) =>
        db.PriceLists.AsNoTracking().Include(p => p.Customer).OrderBy(p => p.Code).ToListAsync(ct);

    public Task<PriceList?> GetAsync(int id, CancellationToken ct = default) =>
        db.PriceLists
            .Include(p => p.Customer)
            .Include(p => p.Revisions).ThenInclude(r => r.Items).ThenInclude(i => i.Route)
            .Include(p => p.Revisions).ThenInclude(r => r.Items).ThenInclude(i => i.VehicleType)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task SaveAsync(PriceList list, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.PriceLists, list.Id == 0);
        PriceListDomainService.EnsureCanSave(list);
        if (await db.PriceLists.AnyAsync(p => p.Code == list.Code && p.Id != list.Id, ct))
            throw new InvalidOperationException("Mã bảng giá đã tồn tại.");
        if (list.CustomerId is int customerId
            && !await db.Customers.AnyAsync(c => c.Id == customerId, ct))
            throw new InvalidOperationException("Khách hàng không tồn tại.");
        var keepCustomerId = list.CustomerId;
        list.Customer = null;
        list.CustomerId = keepCustomerId;
        if (list.Id == 0)
        {
            list.CreatedAt = DateTime.Today;
            db.Add(list);
        }
        else
        {
            var originalVersion = list.RowVersion.ToArray();
            db.Update(list);
            db.ApplyOriginalRowVersion(list, originalVersion);
        }
        await ConcurrencyConflict.SaveAsync(db, ct);
    }

    public async Task SaveRevisionAsync(PriceListRevision revision, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.PriceLists, revision.Id == 0);
        var keepPriceListId = revision.PriceListId;
        var keepEmployeeId = revision.EmployeeId;
        revision.PriceList = null;
        revision.Employee = null;
        revision.PriceListId = keepPriceListId;
        revision.EmployeeId = keepEmployeeId;
        if (revision.Id == 0)
        {
            revision.CreatedAt = DateTime.Today;
            db.Add(revision);
        }
        else db.Update(revision);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task AddItemAsync(PriceListItem item, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.PriceLists, PermissionAction.Update);
        PriceListItemRules.EnsureCanSave(item);
        if (!await db.Routes.AnyAsync(r => r.Id == item.RouteId, ct))
            throw new InvalidOperationException("Tuyến không tồn tại.");
        var keepRevisionId = item.PriceListRevisionId;
        var routeId = item.RouteId;
        var vehicleType = item.VehicleTypeId;
        item.Route = null;
        item.VehicleType = null;
        item.PriceListRevision = null;
        item.PriceListRevisionId = keepRevisionId;
        item.RouteId = routeId;
        item.VehicleTypeId = vehicleType;
        db.Add(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteItemAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.PriceLists, PermissionAction.Delete);
        var item = await db.FindAsync<PriceListItem>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy dòng giá.");
        db.Remove(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task<FreightQuote?> GetFreightAsync(
        int? customerId, int? routeId, int? vehicleTypeId, DateTime asOf,
        CancellationToken ct = default)
    {
        if (routeId is null or <= 0 || vehicleTypeId is null) return null;

        var items = await db.PriceListItems
            .AsNoTracking()
            .Include(i => i.Route)
            .Include(i => i.VehicleType)
            .Include(i => i.PriceListRevision).ThenInclude(r => r!.PriceList)!.ThenInclude(p => p!.Customer)
            .Where(i => i.RouteId == routeId && i.VehicleTypeId == vehicleTypeId)
            .ToListAsync(ct);

        var hit = PriceListMatchRules.Pick(items, customerId, asOf);
        return hit is null ? null : PriceListMatchRules.ToQuote(hit);
    }
}
