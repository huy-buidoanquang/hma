using Hma.Application.Abstractions.Persistence;
using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Features.Catalogs;
using Hma.Domain.Normalization;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Routes;

public class LocationAliasService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<LocationAliasSummary>> ListAsync(CancellationToken ct = default) =>
        db.LocationAliases.AsNoTracking()
            .OrderBy(a => a.Alias)
            .Take(500)
            .Select(a => new LocationAliasSummary(a.Id, a.Alias, a.LocationId, a.Kind,
                a.Location == null ? null : new LocationOption(
                    a.Location.Id, a.Location.Code, a.Location.Name, a.Location.CityId, null)))
            .ToListAsync(ct);

    public async Task SaveAsync(SaveLocationAliasCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Settings, command.Id == 0);
        var alias = command.Id == 0
            ? new LocationAlias()
            : await db.FindAsync<LocationAlias>(command.Id, ct) ?? throw new InvalidOperationException("Không tìm thấy bí danh điểm.");
        alias.Alias = command.Alias;
        alias.LocationId = command.LocationId;
        alias.Kind = command.Kind;
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

        if (alias.Id == 0) db.Add(alias);
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
