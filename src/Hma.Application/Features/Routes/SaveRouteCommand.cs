namespace Hma.Application.Features.Routes;

public sealed record SaveRouteCommand(
    int Id, string Code, string Name, string Fingerprint, string? Description, IReadOnlyList<SaveRouteStopCommand> Stops);
