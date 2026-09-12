namespace Hma.Application.Features.Routes;

public sealed record RouteSummary(
    int Id, string Code, string Name, string Fingerprint, string? Description, IReadOnlyList<RouteStopSummary> Stops);
