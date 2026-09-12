using Hma.Domain.Enums;

namespace Hma.Application.Features.Pricing;

public sealed record SavePriceListFluctuationCommand(
    int Id,
    int PriceListId,
    PriceFluctuationType Type,
    decimal Value,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string Reason,
    byte[] VersionToken);
