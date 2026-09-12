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

public class RouteService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<RouteSummary>> ListAsync(CancellationToken ct = default) =>
        db.Routes.AsNoTracking()
            .OrderBy(r => r.Code)
            .Select(r => new RouteSummary(r.Id, r.Code, r.Name, r.Fingerprint, r.Description,
                r.Stops.OrderBy(s => s.Sequence).Select(s => new RouteStopSummary(
                    s.Id, s.Sequence, s.LocationId,
                    s.Location == null ? null : new LocationOption(
                        s.Location.Id, s.Location.Code, s.Location.Name, s.Location.CityId, null))).ToList()))
            .ToListAsync(ct);

    public Task<Route?> GetAsync(int id, CancellationToken ct = default) =>
        db.Routes
            .Include(r => r.Stops).ThenInclude(s => s.Location)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task SaveAsync(SaveRouteCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Routes, command.Id == 0);
        var route = command.Id == 0
            ? new Route()
            : await db.FindAsync<Route>(command.Id, ct) ?? throw new InvalidOperationException("Không tìm thấy tuyến.");
        route.Code = command.Code;
        route.Name = command.Name;
        route.Fingerprint = command.Fingerprint;
        route.Description = command.Description;
        var incoming = command.Stops.OrderBy(s => s.Sequence)
            .Select(s => new RouteStop { Sequence = s.Sequence, LocationId = s.LocationId }).ToList();
        RouteRules.Renumber(incoming);
        route.Stops.Clear();
        foreach (var stop in incoming)
            route.Stops.Add(stop);
        foreach (var stop in route.Stops)
        {
            var locationId = stop.LocationId;
            stop.Location = null;
            stop.Route = null;
            stop.LocationId = locationId;
        }

        var locationIds = route.Stops.Select(s => s.LocationId).Distinct().ToList();
        var names = await db.Locations.AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.Name, ct);
        if (names.Count != locationIds.Count)
            throw new InvalidOperationException("Điểm trên tuyến không tồn tại.");
        route.Name = RouteFingerprint.FormatLabel(
            route.Stops.OrderBy(s => s.Sequence).Select(s => names.GetValueOrDefault(s.LocationId)));

        RouteRules.EnsureCanSave(route);
        if (await db.Routes.AnyAsync(r => r.Code == route.Code && r.Id != route.Id, ct))
            throw new InvalidOperationException("Mã tuyến đã tồn tại.");
        if (await db.Routes.AnyAsync(r => r.Fingerprint == route.Fingerprint && r.Id != route.Id, ct))
            throw new InvalidOperationException("Tuyến với cùng các điểm theo thứ tự này đã tồn tại.");

        var keepStops = route.Stops.ToList();
        route.Stops.Clear();

        if (route.Id == 0) db.Add(route);
        await PersistenceGuard.SaveAsync(db, ct);

        var oldStops = await db.RouteStops.Where(s => s.RouteId == route.Id).ToListAsync(ct);
        foreach (var stop in oldStops)
            db.Remove(stop);
        foreach (var stop in keepStops)
        {
            db.Add(new RouteStop
            {
                RouteId = route.Id,
                Sequence = stop.Sequence,
                LocationId = stop.LocationId
            });
        }
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Routes, PermissionAction.Delete);
        var entity = await db.FindAsync<Route>(id, ct)
            ?? throw new InvalidOperationException("Không tìm thấy tuyến.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, entity, ct);
    }
}
