using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class RouteAliasService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<RouteAlias>> ListAsync(CancellationToken ct = default) =>
        db.RouteAliases.AsNoTracking()
            .Include(a => a.Route)
            .OrderBy(a => a.Alias)
            .Take(500)
            .ToListAsync(ct);

    public async Task SaveAsync(RouteAlias alias, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Settings, alias.Id == 0);
        alias.Alias = AliasText.Normalize(alias.Alias);
        RouteAliasRules.EnsureCanSave(alias.Alias, alias.RouteId);

        var otherKeys = await OtherAliasKeysAsync(alias.Id, isLocation: false, ct);
        AliasDictionaryRules.EnsureUniqueKey(alias.Alias, otherKeys);

        var locations = await db.Locations.AsNoTracking().Select(l => new { l.Code, l.Name }).ToListAsync(ct);
        var routes = await db.Routes.AsNoTracking().Select(r => new { r.Code, r.Name }).ToListAsync(ct);
        AliasDictionaryRules.EnsureNotCatalog(alias.Alias, locations.Select(l => (l.Code, l.Name)), "điểm");
        AliasDictionaryRules.EnsureNotCatalog(alias.Alias, routes.Select(r => (r.Code, r.Name)), "tuyến");
        if (!await db.Routes.AnyAsync(r => r.Id == alias.RouteId, ct))
            throw new InvalidOperationException("Tuyến đích không tồn tại.");

        var routeId = alias.RouteId;
        alias.Route = null;
        alias.RouteId = routeId;
        if (alias.Id == 0) db.Add(alias);
        else db.Update(alias);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Settings, PermissionAction.Delete);
        var entity = await db.FindAsync<RouteAlias>(id, ct)
            ?? throw new InvalidOperationException("Không tìm thấy bí danh tuyến.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, ct);
    }

    internal static async Task<List<string>> LoadKeysAsync(IHmaDbContext db, int ignoreId, bool ignoreLocation, CancellationToken ct)
    {
        var locationKeys = await db.LocationAliases.AsNoTracking()
            .Where(a => !ignoreLocation || a.Id != ignoreId)
            .Select(a => a.Alias)
            .ToListAsync(ct);
        var routeKeys = await db.RouteAliases.AsNoTracking()
            .Where(a => ignoreLocation || a.Id != ignoreId)
            .Select(a => a.Alias)
            .ToListAsync(ct);
        return locationKeys.Concat(routeKeys).ToList();
    }

    private Task<List<string>> OtherAliasKeysAsync(int id, bool isLocation, CancellationToken ct) =>
        LoadKeysAsync(db, id, isLocation, ct);
}
