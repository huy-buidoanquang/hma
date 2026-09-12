using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Domain.Entities;

namespace Hma.Infrastructure.SqlServer.Tests;

internal sealed class MutableCurrentUser(AppUser user) : ICurrentUser
{
    public AppUser User { get; set; } = user;

    public int? UserId => User.Id;
    public int? EmployeeId => User.EmployeeId;
    public string? UserName => User.UserName;
    public string? DisplayName => User.DisplayName;
    public bool IsManager => User.IsManager;

    public bool Can(string screenKey, PermissionAction action)
    {
        if (User.IsManager)
            return true;
        var permission = User.Permissions.FirstOrDefault(x => x.AppScreen?.Key == screenKey);
        if (permission is null)
            return false;
        return action switch
        {
            PermissionAction.Create => permission.CanCreate,
            PermissionAction.Delete => permission.CanDelete,
            PermissionAction.Update => permission.CanUpdate,
            PermissionAction.View => permission.CanView,
            PermissionAction.Print => permission.CanPrint,
            _ => false
        };
    }
}
