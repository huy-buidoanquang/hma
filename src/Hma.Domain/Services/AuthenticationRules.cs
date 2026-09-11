using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class AuthenticationRules
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public static bool IsLocked(AppUser user, DateTime now) =>
        user.LockoutEnd is not null && user.LockoutEnd > now;

    public static void RegisterFailure(AppUser user, DateTime now)
    {
        user.FailedLoginCount++;
        if (user.FailedLoginCount >= MaxFailedAttempts)
            user.LockoutEnd = now.Add(LockoutDuration);
    }

    public static void RegisterSuccess(AppUser user, DateTime now)
    {
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = now;
    }
}
