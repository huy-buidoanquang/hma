namespace Hma.Application.Features.Catalogs;

public sealed record VehicleSummary(
    int Id,
    string PlateNumber,
    int PartnerId,
    PartnerOption? Partner,
    int? VehicleTypeId,
    VehicleTypeOption? VehicleType,
    decimal? Tonnage);
