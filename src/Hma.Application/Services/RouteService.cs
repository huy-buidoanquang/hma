using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class RouteService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<Route>> ListAsync(CancellationToken ct = default) =>
        db.Routes.AsNoTracking()
            .Include(r => r.Stops).ThenInclude(s => s.Location)
            .OrderBy(r => r.Code)
            .ToListAsync(ct);

    public Task<Route?> GetAsync(int id, CancellationToken ct = default) =>
        db.Routes
            .Include(r => r.Stops).ThenInclude(s => s.Location)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task SaveAsync(Route route, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Routes, route.Id == 0);
        var incoming = route.Stops.OrderBy(s => s.Sequence).ToList();
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
        else db.Update(route);
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
        await ReferentialConflict.SaveAsync(db, ct);
    }
}
