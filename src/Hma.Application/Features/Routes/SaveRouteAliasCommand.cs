namespace Hma.Application.Features.Routes;

public sealed record SaveRouteAliasCommand(int Id, string Alias, int RouteId);
