using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class AppUserRulesTests
{
    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase12!")]
    [InlineData("ALLUPPERCASE12!")]
    [InlineData("NoNumberHere!")]
    [InlineData("NoSpecial1234")]
    public void Password_policy_rejects_weak_password(string password) =>
        Assert.Throws<InvalidOperationException>(() =>
            AppUserRules.EnsurePasswordIsStrong(password));

    [Fact]
    public void Password_policy_accepts_strong_password() =>
        AppUserRules.EnsurePasswordIsStrong("Hma-Secure-2026!");
}
