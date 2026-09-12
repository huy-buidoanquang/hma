using Hma.Application.Features.Catalogs;

namespace Hma.Application.Features.Routes;

public sealed record LocationSummary(
    int Id, string Code, string Name, string? Description, int? CityId, CatalogOption? City);
