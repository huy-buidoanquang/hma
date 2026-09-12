using Hma.Application.Common.Authorization;

namespace Hma.Application.Features.Authentication;

public sealed record AuthenticatedUser(
    int Id,
    int? EmployeeId,
    string UserName,
    string? DisplayName,
    bool IsManager,
    IReadOnlyDictionary<string, PermissionGrant> Permissions)
{
    public bool Can(string screenKey, PermissionAction action) =>
        IsManager || Permissions.TryGetValue(screenKey, out var grant) && grant.Allows(action);
}
