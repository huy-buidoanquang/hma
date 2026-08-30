using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class UserAdminService(IHmaDbContext db, IPasswordHasher hasher, ICurrentUser current)
{
    public Task<List<AppUser>> ListAsync(CancellationToken ct = default) =>
        db.Users.AsNoTracking().Include(u => u.Permissions).ThenInclude(p => p.AppScreen).OrderBy(u => u.UserName).ToListAsync(ct);

    public Task<List<AppScreen>> ScreensAsync(CancellationToken ct = default) =>
        db.Screens.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);

    public async Task SaveAsync(AppUser user, string? newPassword, IEnumerable<UserPermission> permissions, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Users, user.Id == 0);
        if (string.IsNullOrWhiteSpace(user.UserName))
            throw new InvalidOperationException("Tên đăng nhập là bắt buộc.");
        if (await db.Users.AnyAsync(u => u.UserName == user.UserName && u.Id != user.Id, ct))
            throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

        if (!string.IsNullOrWhiteSpace(newPassword))
            user.PasswordHash = hasher.Hash(newPassword);
        else if (user.Id == 0)
            throw new InvalidOperationException("Mật khẩu là bắt buộc khi tạo user.");

        if (user.Id == 0)
        {
            user.CreatedAt = DateTime.Now;
            db.Add(user);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            var existing = await db.UserPermissions.Where(p => p.AppUserId == user.Id).ToListAsync(ct);
            foreach (var p in existing) db.Remove(p);
            db.Update(user);
            await db.SaveChangesAsync(ct);
        }

        foreach (var p in permissions)
        {
            p.Id = 0;
            p.AppUserId = user.Id;
            p.AppScreen = null;
            db.Add(p);
        }
        await db.SaveChangesAsync(ct);
    }
}
