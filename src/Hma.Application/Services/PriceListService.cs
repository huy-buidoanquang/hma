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
            .Include(p => p.Revisions).ThenInclude(r => r.Items).ThenInclude(i => i.DeliveryLocation)
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
            var existing = await db.PriceLists.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == list.Id, ct)
                ?? throw new InvalidOperationException("Không tìm thấy bảng giá.");
            PriceListDomainService.EnsureCanModify(existing);
            if (list.IsLocked)
                throw new InvalidOperationException("Dùng thao tác khóa bảng giá để chốt dữ liệu.");
            var originalVersion = list.RowVersion.ToArray();
            db.Update(list);
            db.ApplyOriginalRowVersion(list, originalVersion);
        }
        await ConcurrencyConflict.SaveAsync(db, ct);
    }

    public async Task SaveRevisionAsync(PriceListRevision revision, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.PriceLists, revision.Id == 0);
        var priceList = await db.PriceLists.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == revision.PriceListId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy bảng giá.");
        PriceListDomainService.EnsureCanModify(priceList);
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
        var revision = await db.PriceListRevisions.AsNoTracking()
            .Include(x => x.PriceList)
            .FirstOrDefaultAsync(x => x.Id == item.PriceListRevisionId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy phiên bản bảng giá.");
        PriceListDomainService.EnsureCanModify(revision.PriceList
            ?? throw new InvalidOperationException("Không tìm thấy bảng giá."));
        if (item.RouteId is int routeIdValue && !await db.Routes.AnyAsync(r => r.Id == routeIdValue, ct))
            throw new InvalidOperationException("Tuyến không tồn tại.");
        if (item.DeliveryLocationId is int locationId
            && !await db.Locations.AnyAsync(x => x.Id == locationId, ct))
            throw new InvalidOperationException("Điểm đến không tồn tại.");
        if (await db.PriceListItems.AnyAsync(x =>
                x.PriceListRevisionId == item.PriceListRevisionId
                && x.RouteId == item.RouteId
                && x.DeliveryLocationId == item.DeliveryLocationId
                && x.VehicleTypeId == item.VehicleTypeId, ct))
            throw new InvalidOperationException("Dòng giá cho tuyến và loại xe này đã tồn tại trong phiên bản.");
        var keepRevisionId = item.PriceListRevisionId;
        var routeId = item.RouteId;
        var deliveryLocationId = item.DeliveryLocationId;
        var vehicleType = item.VehicleTypeId;
        item.Route = null;
        item.DeliveryLocation = null;
        item.VehicleType = null;
        item.PriceListRevision = null;
        item.PriceListRevisionId = keepRevisionId;
        item.RouteId = routeId;
        item.DeliveryLocationId = deliveryLocationId;
        item.VehicleTypeId = vehicleType;
        db.Add(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteItemAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.PriceLists, PermissionAction.Delete);
        var item = await db.FindAsync<PriceListItem>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy dòng giá.");
        var revision = await db.PriceListRevisions.AsNoTracking()
            .Include(x => x.PriceList)
            .FirstOrDefaultAsync(x => x.Id == item.PriceListRevisionId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy phiên bản bảng giá.");
        PriceListDomainService.EnsureCanModify(revision.PriceList
            ?? throw new InvalidOperationException("Không tìm thấy bảng giá."));
        db.Remove(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task LockAsync(int id, string? reason, CancellationToken ct = default)
    {
        if (current.User?.IsManager != true)
            throw new InvalidOperationException("Chỉ quản lý được khóa bảng giá.");
        PermissionGuard.Require(current, ScreenKeys.PriceLists, PermissionAction.Update);
        var list = await db.PriceLists.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("Không tìm thấy bảng giá.");
        list.LockReason = reason?.Trim();
        PriceListDomainService.EnsureCanLock(list);
        list.IsLocked = true;
        list.LockedAt = DateTime.Now;
        await ConcurrencyConflict.SaveAsync(db, ct);
    }

    public async Task<FreightQuote?> GetFreightAsync(
        int? customerId, int? routeId, int? vehicleTypeId, DateTime asOf,
        CancellationToken ct = default)
    {
        if (routeId is null or <= 0 || vehicleTypeId is null) return null;

        var deliveryLocationId = await db.RouteStops.AsNoTracking()
            .Where(x => x.RouteId == routeId)
            .OrderByDescending(x => x.Sequence)
            .Select(x => (int?)x.LocationId)
            .FirstOrDefaultAsync(ct);

        var items = await db.PriceListItems
            .AsNoTracking()
            .Include(i => i.Route)
            .Include(i => i.DeliveryLocation)
            .Include(i => i.VehicleType)
            .Include(i => i.PriceListRevision).ThenInclude(r => r!.PriceList)!.ThenInclude(p => p!.Customer)
            .Where(i => i.VehicleTypeId == vehicleTypeId
                        && (i.RouteId == routeId
                            || (i.RouteId == null && i.DeliveryLocationId == deliveryLocationId)))
            .ToListAsync(ct);

        var hit = PriceListMatchRules.Pick(items, customerId, asOf, routeId);
        return hit is null ? null : PriceListMatchRules.ToQuote(hit);
    }
}
