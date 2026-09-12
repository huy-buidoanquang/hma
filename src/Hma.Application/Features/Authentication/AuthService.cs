using Hma.Application.Abstractions.Persistence;
using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Authentication;

public class AuthService(IHmaDbContext db, IPasswordHasher hasher, TimeProvider timeProvider) : IAuthService
{
    public async Task<AuthenticatedUser?> AuthenticateAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.Permissions).ThenInclude(p => p.AppScreen)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
        if (user is null)
            return null;

        var now = timeProvider.GetLocalNow().DateTime;
        if (AuthenticationRules.IsLocked(user, now))
            return null;

        if (!hasher.Verify(user.PasswordHash, password))
        {
            AuthenticationRules.RegisterFailure(user, now);
            db.Add(new ChangeLog
            {
                EntityName = "AppUser",
                EntityId = user.Id,
                Action = "LoginFailed",
                Summary = "Đăng nhập thất bại.",
                UserId = user.Id,
                ChangedAt = now
            });
            await PersistenceGuard.SaveAsync(db, cancellationToken);
            return null;
        }

        AuthenticationRules.RegisterSuccess(user, now);
        db.Add(new ChangeLog
        {
            EntityName = "AppUser",
            EntityId = user.Id,
            Action = "LoginSucceeded",
            Summary = "Đăng nhập thành công.",
            UserId = user.Id,
            ChangedAt = now
        });
        await PersistenceGuard.SaveAsync(db, cancellationToken);
        return new AuthenticatedUser(
            user.Id,
            user.EmployeeId,
            user.UserName,
            user.DisplayName,
            user.IsManager,
            user.Permissions
                .Where(x => x.AppScreen is not null)
                .ToDictionary(
                    x => x.AppScreen!.Key,
                    x => new PermissionGrant(
                        x.CanCreate,
                        x.CanDelete,
                        x.CanUpdate,
                        x.CanView,
                        x.CanPrint),
                    StringComparer.Ordinal));
    }
}
