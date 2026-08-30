using Hma.Application.Abstractions;

namespace Hma.Application.Services;

public static class PermissionGuard
{
    public static void Require(ICurrentUser current, string screenKey, PermissionAction action)
    {
        if (current.Can(screenKey, action))
            return;
        throw new InvalidOperationException("Bạn không có quyền thực hiện thao tác này.");
    }

    public static void RequireSave(ICurrentUser current, string screenKey, bool isNew) =>
        Require(current, screenKey, isNew ? PermissionAction.Create : PermissionAction.Update);
}
