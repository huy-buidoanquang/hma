using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class AppUserDomainServiceTests
{
    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase12!")]
    [InlineData("ALLUPPERCASE12!")]
    [InlineData("NoNumberHere!")]
    [InlineData("NoSpecial1234")]
    public void Password_policy_rejects_weak_password(string password) =>
        Assert.Throws<InvalidOperationException>(() =>
            AppUserDomainService.EnsurePasswordIsStrong(password));

    [Fact]
    public void Password_policy_accepts_strong_password() =>
        AppUserDomainService.EnsurePasswordIsStrong("Hma-Secure-2026!");
}
