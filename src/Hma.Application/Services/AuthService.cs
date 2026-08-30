using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class AuthService(IHmaDbContext db, IPasswordHasher hasher, ICurrentUser current) : IAuthService
{
    public async Task<AppUser?> LoginAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.Permissions).ThenInclude(p => p.AppScreen)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
        if (user is null || !hasher.Verify(user.PasswordHash, password))
            return null;
        current.User = user;
        return user;
    }

    public void Logout() => current.User = null;
}
