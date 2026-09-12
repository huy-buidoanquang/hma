namespace Hma.Application.Features.Catalogs;

public sealed record VehicleOption(
    int Id,
    string PlateNumber,
    int PartnerId,
    PartnerOption? Partner,
    int? VehicleTypeId,
    VehicleTypeOption? VehicleType,
    decimal? Tonnage);
