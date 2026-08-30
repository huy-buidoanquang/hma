using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class EmailRulesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_is_optional(string? email) => Assert.True(EmailRules.IsValid(email));

    [Theory]
    [InlineData("a@b.co")]
    [InlineData("ke.toan@haminhanh.vn")]
    [InlineData("User.Name+tag@example.com")]
    public void Accepts_common_addresses(string email) => Assert.True(EmailRules.IsValid(email));

    [Theory]
    [InlineData("que")]
    [InlineData("a@b")]
    [InlineData("a b@c.com")]
    [InlineData("a@@b.com")]
    [InlineData("a..b@c.com")]
    public void Rejects_invalid(string email) => Assert.False(EmailRules.IsValid(email));
}
