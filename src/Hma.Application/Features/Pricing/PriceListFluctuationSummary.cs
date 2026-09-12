using Hma.Domain.Enums;

namespace Hma.Application.Features.Pricing;

public sealed record PriceListFluctuationSummary(
    int Id,
    int PriceListId,
    PriceFluctuationType Type,
    decimal Value,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string Reason,
    DateTime CreatedAt,
    int? CreatedByUserId,
    string? CreatedByDisplayName,
    byte[] VersionToken)
{
    public string ValueLabel => Type == PriceFluctuationType.Percentage
        ? $"{Value:+0.####;-0.####}%"
        : $"{Value:+#,##0.##;-#,##0.##} đ";
}
