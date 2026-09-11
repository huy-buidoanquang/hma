using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class LocationAliasService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<LocationAlias>> ListAsync(CancellationToken ct = default) =>
        db.LocationAliases.AsNoTracking()
            .Include(a => a.Location)
            .OrderBy(a => a.Alias)
            .Take(500)
            .ToListAsync(ct);

    public async Task SaveAsync(LocationAlias alias, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Settings, alias.Id == 0);
        alias.Alias = AliasText.Normalize(alias.Alias);
        LocationAliasRules.EnsureCanSave(alias.Alias, alias.LocationId, alias.Kind);

        var otherKeys = await RouteAliasService.LoadKeysAsync(db, alias.Id, ignoreLocation: true, ct);
        LocationAliasRules.EnsureUnique(alias.Alias, otherKeys);

        var locations = await db.Locations.AsNoTracking().Select(l => new { l.Code, l.Name }).ToListAsync(ct);
        var routes = await db.Routes.AsNoTracking().Select(r => new { r.Code, r.Name }).ToListAsync(ct);
        AliasDictionaryRules.EnsureNotCatalog(alias.Alias, locations.Select(l => (l.Code, l.Name)), "điểm");
        AliasDictionaryRules.EnsureNotCatalog(alias.Alias, routes.Select(r => (r.Code, r.Name)), "tuyến");
        if (!await db.Locations.AnyAsync(l => l.Id == alias.LocationId, ct))
            throw new InvalidOperationException("Điểm đích không tồn tại.");

        var locationId = alias.LocationId;
        alias.Location = null;
        alias.LocationId = locationId;
        if (alias.Id == 0) db.Add(alias);
        else db.Update(alias);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Settings, PermissionAction.Delete);
        var entity = await db.FindAsync<LocationAlias>(id, ct)
            ?? throw new InvalidOperationException("Không tìm thấy bí danh điểm.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, entity, ct);
    }
}
