using Hma.Application.Common.Authorization;
using Hma.Application.Features.Authentication;

namespace Hma.Application.Tests;

public class AuthenticatedUserTests
{
    [Fact]
    public void Manager_is_authorized_without_explicit_grants()
    {
        var principal = new AuthenticatedUser(1, null, "manager", "Quản lý", true,
            new Dictionary<string, PermissionGrant>());

        Assert.True(principal.Can(ScreenKeys.DispatchOrders, PermissionAction.Delete));
    }

    [Fact]
    public void Principal_uses_the_matching_immutable_permission_grant()
    {
        var principal = new AuthenticatedUser(2, 4, "operator", "Điều hành", false,
            new Dictionary<string, PermissionGrant>
            {
                [ScreenKeys.DispatchOrders] = new(true, false, true, true, false)
            });

        Assert.True(principal.Can(ScreenKeys.DispatchOrders, PermissionAction.Update));
        Assert.False(principal.Can(ScreenKeys.DispatchOrders, PermissionAction.Delete));
        Assert.False(principal.Can(ScreenKeys.Customers, PermissionAction.View));
    }
}
