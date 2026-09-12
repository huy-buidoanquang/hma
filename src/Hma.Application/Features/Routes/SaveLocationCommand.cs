namespace Hma.Application.Features.Routes;

public sealed record SaveLocationCommand(int Id, string Code, string Name, string? Description, int? CityId);
