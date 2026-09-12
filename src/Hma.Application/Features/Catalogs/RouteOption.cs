namespace Hma.Application.Features.Catalogs;

public sealed record RouteOption(int Id, string Code, string Name, IReadOnlyList<RouteStopOption> Stops);
