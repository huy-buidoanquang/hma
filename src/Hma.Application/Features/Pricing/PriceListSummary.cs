using Hma.Application.Features.Customers;

namespace Hma.Application.Features.Pricing;

public sealed record PriceListSummary(
    int Id,
    string Code,
    string Name,
    string? Description,
    int? CustomerId,
    CustomerOption? Customer,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    bool HasPriceFluctuation,
    bool IsLocked,
    string? LockReason,
    byte[] VersionToken);
