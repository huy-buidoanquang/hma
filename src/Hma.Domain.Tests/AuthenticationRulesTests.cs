using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class AuthenticationRulesTests
{
    [Fact]
    public void Fifth_failure_locks_account_for_configured_duration()
    {
        var now = new DateTime(2026, 9, 11, 8, 0, 0);
        var user = new AppUser { FailedLoginCount = 4 };

        AuthenticationRules.RegisterFailure(user, now);

        Assert.Equal(5, user.FailedLoginCount);
        Assert.Equal(now.AddMinutes(15), user.LockoutEnd);
        Assert.True(AuthenticationRules.IsLocked(user, now.AddMinutes(14)));
        Assert.False(AuthenticationRules.IsLocked(user, now.AddMinutes(16)));
    }

    [Fact]
    public void Successful_login_clears_failures_and_records_time()
    {
        var now = new DateTime(2026, 9, 11, 8, 0, 0);
        var user = new AppUser
        {
            FailedLoginCount = 3,
            LockoutEnd = now.AddMinutes(5)
        };

        AuthenticationRules.RegisterSuccess(user, now);

        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockoutEnd);
        Assert.Equal(now, user.LastLoginAt);
    }
}
