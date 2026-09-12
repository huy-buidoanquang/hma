using Hma.Application.Abstractions.Persistence;
using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Features.Catalogs;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Authentication;

public class UserAdminService(
    IHmaDbContext db,
    IPasswordHasher hasher,
    ICurrentUser current,
    TimeProvider timeProvider)
{
    public async Task<List<UserSummary>> ListAsync(CancellationToken ct = default)
    {
        var rows = await db.Users.AsNoTracking().Include(u => u.Employee).Include(u => u.Permissions)
            .OrderBy(u => u.UserName).ToListAsync(ct);
        return rows.Select(u => new UserSummary(
            u.Id, u.UserName, u.DisplayName, u.EmployeeId,
            u.Employee is null ? null : new EmployeeOption(
                u.Employee.Id, u.Employee.Code, u.Employee.Name, u.Employee.DepartmentId, null,
                u.Employee.JobTitleId, null),
            u.IsManager, u.IsSpecial,
            u.Permissions.Select(p => new UserPermissionDetails(p.AppScreenId,
                new PermissionGrant(p.CanCreate, p.CanDelete, p.CanUpdate, p.CanView, p.CanPrint))).ToList()))
            .ToList();
    }

    public Task<List<ScreenSummary>> ScreensAsync(CancellationToken ct = default) =>
        db.Screens.AsNoTracking().OrderBy(s => s.Name)
            .Select(s => new ScreenSummary(s.Id, s.Key, s.Name)).ToListAsync(ct);

    public async Task SaveAsync(SaveUserCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Users, command.Id == 0);
        var user = command.Id == 0
            ? new AppUser()
            : await db.FindAsync<AppUser>(command.Id, ct) ?? throw new InvalidOperationException("Không tìm thấy người dùng.");
        user.UserName = command.UserName;
        user.DisplayName = command.DisplayName;
        user.EmployeeId = command.EmployeeId;
        user.IsManager = command.IsManager;
        user.IsSpecial = command.IsSpecial;
        AppUserRules.EnsureCanSave(user, passwordRequired: user.Id == 0 && string.IsNullOrWhiteSpace(command.NewPassword));
        if (await db.Users.AnyAsync(u => u.UserName == user.UserName && u.Id != user.Id, ct))
            throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

        if (!string.IsNullOrWhiteSpace(command.NewPassword))
        {
            AppUserRules.EnsurePasswordIsStrong(command.NewPassword);
            user.PasswordHash = hasher.Hash(command.NewPassword);
            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
        }

        if (user.EmployeeId is int employeeId
            && !await db.Employees.AnyAsync(e => e.Id == employeeId, ct))
            throw new InvalidOperationException("Nhân viên không tồn tại.");

        if (user.Id == 0)
        {
            user.CreatedAt = timeProvider.GetLocalNow().DateTime;
            db.Add(user);
            await PersistenceGuard.SaveAsync(db, ct);
        }
        else
        {
            var existing = await db.UserPermissions.Where(p => p.AppUserId == user.Id).ToListAsync(ct);
            foreach (var p in existing) db.Remove(p);
            await PersistenceGuard.SaveAsync(db, ct);
        }

        foreach (var permission in command.Permissions)
        {
            db.Add(new UserPermission
            {
                AppUserId = user.Id,
                AppScreenId = permission.ScreenId,
                CanView = permission.Grant.CanView,
                CanCreate = permission.Grant.CanCreate,
                CanUpdate = permission.Grant.CanUpdate,
                CanDelete = permission.Grant.CanDelete,
                CanPrint = permission.Grant.CanPrint,
            });
        }
        await PersistenceGuard.SaveAsync(db, ct);
    }
}
