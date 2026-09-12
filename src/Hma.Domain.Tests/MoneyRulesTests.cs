using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class MoneyRulesTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(1234, "1,234")]
    [InlineData(1_234_567, "1,234,567")]
    [InlineData(1_234_567.9, "1,234,568")]
    public void Format_uses_comma_groups(decimal amount, string expected) =>
        Assert.Equal(expected, MoneyRules.Format(amount));

    [Theory]
    [InlineData("1,234,567", 1234567)]
    [InlineData("1234567", 1234567)]
    [InlineData("1.234.567", 1234567)]
    [InlineData("1,234,567 VNĐ", 1234567)]
    [InlineData("1234.5", 1234.5)]
    [InlineData("1,234.50", 1234.50)]
    public void Parse_accepts_vnd_and_comma_groups(string text, double expected) =>
        Assert.True(MoneyRules.TryParse(text, out var amount) && amount == (decimal)expected);

    [Fact]
    public void Parse_empty_fails() => Assert.False(MoneyRules.TryParse("  ", out _));

    [Fact]
    public void EnsureNonNegative_rejects_negative()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MoneyRules.EnsureNonNegative(-1, "Đơn giá"));
        Assert.Contains("không được âm", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsurePositive_rejects_zero()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MoneyRules.EnsurePositive(0, "Số tiền"));
        Assert.Contains("lớn hơn 0", ex.Message, StringComparison.Ordinal);
    }
}
