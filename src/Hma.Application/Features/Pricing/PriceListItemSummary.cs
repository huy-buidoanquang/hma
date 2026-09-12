using Hma.Application.Features.Catalogs;

namespace Hma.Application.Features.Pricing;

public sealed record PriceListItemSummary(
    int Id,
    int PriceListRevisionId,
    int? RouteId,
    RouteOption? Route,
    int? DeliveryLocationId,
    LocationOption? DeliveryLocation,
    int VehicleTypeId,
    VehicleTypeOption? VehicleType,
    decimal UnitPrice,
    decimal Surcharge);
