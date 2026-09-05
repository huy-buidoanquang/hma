using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class RouteRules
{
    public static void EnsureCanSave(Route route)
    {
        CatalogItemDomainService.EnsureCanSave(route.Code, route.Name, "tuyến");
        var stops = route.Stops.OrderBy(s => s.Sequence).ToList();
        if (stops.Count < 2)
            throw new InvalidOperationException("Tuyến cần ít nhất hai điểm.");
        if (stops.Any(s => s.LocationId <= 0))
            throw new InvalidOperationException("Mỗi điểm trên tuyến phải được chọn.");
        route.Fingerprint = RouteFingerprint.From(stops.Select(s => s.LocationId));
        if (string.IsNullOrWhiteSpace(route.Name))
            route.Name = RouteFingerprint.FormatLabel(stops.Select(s => s.Location?.Name));
    }

    public static void Renumber(IList<RouteStop> stops)
    {
        for (var i = 0; i < stops.Count; i++)
            stops[i].Sequence = i;
    }
}
