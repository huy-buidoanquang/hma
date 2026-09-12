using Hma.Application.Common.Authorization;
using Hma.Application.Features.Authentication;
using Hma.Desktop.Wpf.Abstractions;

namespace Hma.Desktop.Wpf.Infrastructure.Session;

public sealed class DesktopUserSession : IDesktopUserSession
{
    private AuthenticatedUser? _user;

    public int? UserId => _user?.Id;
    public int? EmployeeId => _user?.EmployeeId;
    public string? UserName => _user?.UserName;
    public string? DisplayName => _user?.DisplayName;
    public bool IsManager => _user?.IsManager == true;
    public bool IsAuthenticated => _user is not null;

    public bool Can(string screenKey, PermissionAction action) =>
        _user?.Can(screenKey, action) == true;

    public void SignIn(AuthenticatedUser user) => _user = user;

    public void SignOut() => _user = null;
}
