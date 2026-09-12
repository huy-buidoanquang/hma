using Hma.Application.Features.Catalogs;

namespace Hma.Application.Features.Routes;

public sealed record RouteStopSummary(int Id, int Sequence, int LocationId, LocationOption? Location);
