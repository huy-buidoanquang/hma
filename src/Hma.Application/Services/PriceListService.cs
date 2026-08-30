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
            .Include(p => p.Revisions).ThenInclude(r => r.Items).ThenInclude(i => i.DeliveryCity)
            .Include(p => p.Revisions).ThenInclude(r => r.Items).ThenInclude(i => i.PickupCity)
            .Include(p => p.Revisions).ThenInclude(r => r.Items).ThenInclude(i => i.VehicleType)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task SaveAsync(PriceList list, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.PriceLists, list.Id == 0);
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
        if (revision.Id == 0)
        {
            revision.CreatedAt = DateTime.Today;
            db.Add(revision);
        }
        else db.Update(revision);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddItemAsync(PriceListItem item, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.PriceLists, PermissionAction.Update);
        PriceListItemRules.EnsureCanSave(item);
        db.Add(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteItemAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.PriceLists, PermissionAction.Delete);
        var item = await db.FindAsync<PriceListItem>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy dòng giá.");
        db.Remove(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task<(decimal UnitPrice, decimal Surcharge)?> GetFreightAsync(
        int? customerId, int? pickupCityId, int? deliveryCityId, int? vehicleTypeId, CancellationToken ct = default)
    {
        if (deliveryCityId is null || vehicleTypeId is null) return null;

        var items = await db.PriceListItems
            .AsNoTracking()
            .Include(i => i.PriceListRevision).ThenInclude(r => r!.PriceList)
            .Where(i => i.DeliveryCityId == deliveryCityId
                        && i.VehicleTypeId == vehicleTypeId)
            .ToListAsync(ct);

        var today = DateTime.Today;
        items = items.Where(i =>
        {
            var list = i.PriceListRevision!.PriceList!;
            if (list.EffectiveFrom is { } from && from > today) return false;
            if (list.EffectiveTo is { } to && to < today) return false;
            return true;
        }).ToList();

        PriceListItem? Match(int? wantedCustomer, bool requirePickupExact)
        {
            var subset = items.Where(i => i.PriceListRevision!.PriceList!.CustomerId == wantedCustomer);
            if (requirePickupExact)
                subset = subset.Where(i => i.PickupCityId == pickupCityId);
            else
                subset = subset.Where(i => i.PickupCityId == null);
            return subset.OrderByDescending(i => i.PriceListRevision!.CreatedAt).FirstOrDefault();
        }

        var hit = Match(customerId, true)
                  ?? Match(customerId, false)
                  ?? Match(null, true)
                  ?? Match(null, false);
        return hit is null ? null : (hit.UnitPrice, hit.Surcharge);
    }
}
