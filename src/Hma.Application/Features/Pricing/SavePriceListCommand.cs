namespace Hma.Application.Features.Pricing;

public sealed record SavePriceListCommand(
    int Id,
    string Code,
    string Name,
    string? Description,
    int? CustomerId,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    bool HasPriceFluctuation,
    string? LockReason,
    byte[] VersionToken);
