namespace Hma.Application.Features.Catalogs;

public sealed record SaveVehicleCommand(int Id, string PlateNumber, int PartnerId, int? VehicleTypeId, decimal? Tonnage);
