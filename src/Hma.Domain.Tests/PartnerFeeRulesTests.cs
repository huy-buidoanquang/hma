using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class PartnerFeeRulesTests
{
    [Fact]
    public void RemainderPayable_keeps_share_after_operating_fee()
    {
        Assert.Equal(920_000m, PartnerFeeRules.RemainderPayable(1_000_000m, 8));
        Assert.Equal(1_000_000m, PartnerFeeRules.RemainderPayable(1_000_000m, 0));
        Assert.Equal(0m, PartnerFeeRules.RemainderPayable(1_000_000m, 100));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void EnsurePercent_rejects_outside_0_100(double percent)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => PartnerFeeRules.EnsurePercent((decimal)percent));
        Assert.Contains("0 đến 100", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RemainderPayable_rejects_negative_total()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => PartnerFeeRules.RemainderPayable(-1, 8));
        Assert.Contains("không được âm", ex.Message, StringComparison.Ordinal);
    }
}
