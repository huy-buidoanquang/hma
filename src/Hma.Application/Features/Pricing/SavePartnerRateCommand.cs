namespace Hma.Application.Features.Pricing;

public sealed record SavePartnerRateCommand(
    int Id,
    int PartnerId,
    int RouteId,
    int VehicleTypeId,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    decimal UnitPrice,
    decimal Surcharge,
    byte[] VersionToken);
