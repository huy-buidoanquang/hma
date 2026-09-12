using Hma.Application.Common.Authorization;

namespace Hma.Application.Abstractions.Security;

public interface ICurrentUser
{
    int? UserId { get; }
    int? EmployeeId { get; }
    string? UserName { get; }
    string? DisplayName { get; }
    bool IsManager { get; }
    bool IsAuthenticated => UserId is not null;
    bool Can(string screenKey, PermissionAction action);
}
