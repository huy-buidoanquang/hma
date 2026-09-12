using Hma.Application.Common.Authorization;

namespace Hma.Application.Features.Authentication;

public sealed record PermissionGrant(
    bool CanCreate,
    bool CanDelete,
    bool CanUpdate,
    bool CanView,
    bool CanPrint)
{
    public bool Allows(PermissionAction action) => action switch
    {
        PermissionAction.Create => CanCreate,
        PermissionAction.Delete => CanDelete,
        PermissionAction.Update => CanUpdate,
        PermissionAction.View => CanView,
        PermissionAction.Print => CanPrint,
        _ => false
    };
}
