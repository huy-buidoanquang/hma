namespace Hma.Application.Features.Catalogs;

public sealed record CatalogItemSummary(int Id, string Code, string Name, string? Description = null);
