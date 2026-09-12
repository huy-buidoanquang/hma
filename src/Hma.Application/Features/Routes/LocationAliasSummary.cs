using Hma.Application.Features.Catalogs;

namespace Hma.Application.Features.Routes;

public sealed record LocationAliasSummary(
    int Id, string Alias, int LocationId, LocationAliasKind Kind, LocationOption? Location);
