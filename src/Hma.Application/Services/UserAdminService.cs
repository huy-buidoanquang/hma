using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class UserAdminService(IHmaDbContext db, IPasswordHasher hasher, ICurrentUser current)
{
    public Task<List<AppUser>> ListAsync(CancellationToken ct = default) =>
        db.Users.AsNoTracking().Include(u => u.Employee).Include(u => u.Permissions).ThenInclude(p => p.AppScreen).OrderBy(u => u.UserName).ToListAsync(ct);

    public Task<List<AppScreen>> ScreensAsync(CancellationToken ct = default) =>
        db.Screens.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);

    public async Task SaveAsync(AppUser user, string? newPassword, IEnumerable<UserPermission> permissions, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Users, user.Id == 0);
        AppUserDomainService.EnsureCanSave(user, passwordRequired: user.Id == 0 && string.IsNullOrWhiteSpace(newPassword));
        if (await db.Users.AnyAsync(u => u.UserName == user.UserName && u.Id != user.Id, ct))
            throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

        if (!string.IsNullOrWhiteSpace(newPassword))
            user.PasswordHash = hasher.Hash(newPassword);

        if (user.EmployeeId is int employeeId
            && !await db.Employees.AnyAsync(e => e.Id == employeeId, ct))
            throw new InvalidOperationException("Nhân viên không tồn tại.");

        var keepEmployeeId = user.EmployeeId;
        user.Employee = null;
        user.EmployeeId = keepEmployeeId;

        if (user.Id == 0)
        {
            user.CreatedAt = DateTime.Now;
            db.Add(user);
            await PersistenceGuard.SaveAsync(db, ct);
        }
        else
        {
            var existing = await db.UserPermissions.Where(p => p.AppUserId == user.Id).ToListAsync(ct);
            foreach (var p in existing) db.Remove(p);
            db.Update(user);
            await PersistenceGuard.SaveAsync(db, ct);
        }

        foreach (var p in permissions)
        {
            p.Id = 0;
            p.AppUserId = user.Id;
            p.AppScreen = null;
            db.Add(p);
        }
        await PersistenceGuard.SaveAsync(db, ct);
    }
}
