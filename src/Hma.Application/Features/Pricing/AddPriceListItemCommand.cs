namespace Hma.Application.Features.Pricing;

public sealed record AddPriceListItemCommand(
    int PriceListRevisionId,
    int? RouteId,
    int? DeliveryLocationId,
    int VehicleTypeId,
    decimal UnitPrice,
    decimal Surcharge);
