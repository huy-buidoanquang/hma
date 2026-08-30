using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class PhoneRulesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_is_optional(string? phone) => Assert.True(PhoneRules.IsValid(phone));

    [Theory]
    [InlineData("0901234567")]
    [InlineData("0321234567")]
    [InlineData("0521234567")]
    [InlineData("0701234567")]
    [InlineData("0812345678")]
    [InlineData("02438223322")]
    [InlineData("+84901234567")]
    [InlineData("84901234567")]
    [InlineData("090 123 4567")]
    [InlineData("090-123-4567")]
    [InlineData("090.123.4567")]
    public void Accepts_vietnamese_numbers(string phone) => Assert.True(PhoneRules.IsValid(phone));

    [Theory]
    [InlineData("que")]
    [InlineData("123")]
    [InlineData("012345")]
    [InlineData("0012345678")]
    [InlineData("abc0901234567")]
    [InlineData("090123456")]
    public void Rejects_invalid(string phone) => Assert.False(PhoneRules.IsValid(phone));

    [Fact]
    public void EnsureOptional_throws_vietnamese()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => PhoneRules.EnsureOptional("que"));
        Assert.Contains("không hợp lệ", ex.Message, StringComparison.Ordinal);
    }
}
