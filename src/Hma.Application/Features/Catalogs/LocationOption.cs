namespace Hma.Application.Features.Catalogs;

public sealed record LocationOption(int Id, string Code, string Name, int? CityId, CatalogOption? City);
