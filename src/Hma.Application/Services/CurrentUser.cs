using Hma.Application.Abstractions;
using Hma.Domain.Entities;

namespace Hma.Application.Services;

public sealed class CurrentUser : ICurrentUser
{
    public AppUser? User { get; set; }

    public bool Can(string screenKey, PermissionAction action)
    {
        if (User is null) return false;
        if (User.IsManager) return true;
        var perm = User.Permissions?.FirstOrDefault(p => p.AppScreen?.Key == screenKey);
        if (perm is null) return false;
        return action switch
        {
            PermissionAction.Create => perm.CanCreate,
            PermissionAction.Delete => perm.CanDelete,
            PermissionAction.Update => perm.CanUpdate,
            PermissionAction.View => perm.CanView,
            PermissionAction.Print => perm.CanPrint,
            _ => false
        };
    }
}
