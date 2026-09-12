using Hma.Application.Features.Catalogs;

namespace Hma.Application.Features.Pricing;

public sealed record PartnerRateSummary(
    int Id,
    int PartnerId,
    PartnerOption? Partner,
    int RouteId,
    RouteOption? Route,
    int VehicleTypeId,
    VehicleTypeOption? VehicleType,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    decimal UnitPrice,
    decimal Surcharge,
    DateTime CreatedAt,
    int? CreatedByUserId,
    byte[] VersionToken);
