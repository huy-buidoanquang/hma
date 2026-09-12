namespace Hma.Application.Features.Routes;

public sealed record SaveLocationAliasCommand(int Id, string Alias, int LocationId, LocationAliasKind Kind);
