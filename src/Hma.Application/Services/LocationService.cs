using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class LocationService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<Location>> ListAsync(CancellationToken ct = default) =>
        db.Locations.AsNoTracking().Include(l => l.City).OrderBy(l => l.Code).ToListAsync(ct);

    public async Task SaveAsync(Location location, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Locations, location.Id == 0);
        CatalogItemDomainService.EnsureCanSave(location.Code, location.Name, "điểm");
        if (await db.Locations.AnyAsync(l => l.Code == location.Code && l.Id != location.Id, ct))
            throw new InvalidOperationException("Mã điểm đã tồn tại.");
        if (location.CityId is int cityId && !await db.Cities.AnyAsync(c => c.Id == cityId, ct))
            throw new InvalidOperationException("Thành phố không tồn tại.");
        var keepCityId = location.CityId;
        location.City = null;
        location.CityId = keepCityId;
        if (location.Id == 0) db.Add(location);
        else db.Update(location);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Locations, PermissionAction.Delete);
        var entity = await db.FindAsync<Location>(id, ct)
            ?? throw new InvalidOperationException("Không tìm thấy điểm.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, entity, ct);
    }
}
