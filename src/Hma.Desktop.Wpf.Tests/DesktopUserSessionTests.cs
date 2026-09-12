using Hma.Application.Common.Authorization;
using Hma.Application.Features.Authentication;
using Hma.Desktop.Wpf.Infrastructure.Session;

namespace Hma.Desktop.Wpf.Tests;

public class DesktopUserSessionTests
{
    [Fact]
    public void Sign_out_clears_authenticated_principal()
    {
        var session = new DesktopUserSession();
        session.SignIn(new AuthenticatedUser(
            7,
            3,
            "admin",
            "Quản trị",
            false,
            new Dictionary<string, PermissionGrant>
            {
                [ScreenKeys.Customers] = new(false, false, false, true, false)
            }));

        Assert.True(session.IsAuthenticated);
        Assert.True(session.Can(ScreenKeys.Customers, PermissionAction.View));

        session.SignOut();

        Assert.False(session.IsAuthenticated);
        Assert.Null(session.UserId);
        Assert.False(session.Can(ScreenKeys.Customers, PermissionAction.View));
    }
}
