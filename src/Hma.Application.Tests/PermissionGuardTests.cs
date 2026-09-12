using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using NSubstitute;

namespace Hma.Application.Tests;

public class PermissionGuardTests
{
    [Fact]
    public void Require_allows_when_user_can()
    {
        var current = Substitute.For<ICurrentUser>();
        current.Can(ScreenKeys.Customers, PermissionAction.View).Returns(true);
        PermissionGuard.Require(current, ScreenKeys.Customers, PermissionAction.View);
    }

    [Fact]
    public void Require_throws_vietnamese_when_denied()
    {
        var current = Substitute.For<ICurrentUser>();
        current.Can(ScreenKeys.Customers, PermissionAction.Delete).Returns(false);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PermissionGuard.Require(current, ScreenKeys.Customers, PermissionAction.Delete));
        Assert.Contains("không có quyền", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RequireSave_uses_create_for_new_records()
    {
        var current = Substitute.For<ICurrentUser>();
        current.Can(ScreenKeys.Customers, PermissionAction.Create).Returns(true);
        PermissionGuard.RequireSave(current, ScreenKeys.Customers, isNew: true);
        current.Received(1).Can(ScreenKeys.Customers, PermissionAction.Create);
    }
}
