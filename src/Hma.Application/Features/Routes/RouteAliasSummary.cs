using Hma.Application.Features.Catalogs;

namespace Hma.Application.Features.Routes;

public sealed record RouteAliasSummary(int Id, string Alias, int RouteId, RouteOption? Route);
