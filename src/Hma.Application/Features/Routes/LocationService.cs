using Hma.Application.Abstractions.Persistence;
using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Features.Catalogs;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Routes;

public class LocationService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<LocationSummary>> ListAsync(CancellationToken ct = default) =>
        db.Locations.AsNoTracking().OrderBy(l => l.Code)
            .Select(l => new LocationSummary(l.Id, l.Code, l.Name, l.Description, l.CityId,
                l.City == null ? null : new CatalogOption(l.City.Id, l.City.Code, l.City.Name)))
            .ToListAsync(ct);

    public async Task SaveAsync(SaveLocationCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Locations, command.Id == 0);
        var location = command.Id == 0
            ? new Location()
            : await db.FindAsync<Location>(command.Id, ct) ?? throw new InvalidOperationException("Không tìm thấy điểm.");
        location.Code = command.Code;
        location.Name = command.Name;
        location.Description = command.Description;
        location.CityId = command.CityId;
        CatalogItemRules.EnsureCanSave(location.Code, location.Name, "điểm");
        if (await db.Locations.AnyAsync(l => l.Code == location.Code && l.Id != location.Id, ct))
            throw new InvalidOperationException("Mã điểm đã tồn tại.");
        if (location.CityId is int cityId && !await db.Cities.AnyAsync(c => c.Id == cityId, ct))
            throw new InvalidOperationException("Thành phố không tồn tại.");
        if (location.Id == 0) db.Add(location);
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
