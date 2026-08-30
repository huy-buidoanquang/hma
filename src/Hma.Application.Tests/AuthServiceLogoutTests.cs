using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Application.Tests;

public class AuthServiceLogoutTests
{
    [Fact]
    public void Logout_clears_current_user()
    {
        var current = new CurrentUser { User = new AppUser { UserName = "admin" } };
        var sut = new AuthService(Substitute.For<IHmaDbContext>(), Substitute.For<IPasswordHasher>(), current);

        sut.Logout();

        Assert.Null(current.User);
        Assert.False(((ICurrentUser)current).IsAuthenticated);
    }
}
