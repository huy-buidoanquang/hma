namespace Hma.Application.Features.Catalogs;

public sealed record SaveCatalogItemCommand(int Id, string Code, string Name, string? Description = null);
