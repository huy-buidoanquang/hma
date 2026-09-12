namespace Hma.Application.Features.Dispatching;

public sealed record DispatchOrderLineDetails(
    int Id,
    int LineNumber,
    string? GoodsName,
    int? PackageCount,
    string? Route,
    decimal? Kilometers,
    string? Notes);
