namespace Hma.Domain.Services;

public static class PartnerFeeRules
{
    public static void EnsurePercent(decimal percent)
    {
        if (percent is < 0 or > 100)
            throw new InvalidOperationException("Phí điều hành phải từ 0 đến 100%.");
    }

    public static decimal RemainderPayable(decimal totalAmount, decimal operatingFeePercent)
    {
        EnsurePercent(operatingFeePercent);
        MoneyRules.EnsureNonNegative(totalAmount, "Tổng cước");
        return Math.Round(totalAmount * (1 - operatingFeePercent / 100m), 2);
    }
}
