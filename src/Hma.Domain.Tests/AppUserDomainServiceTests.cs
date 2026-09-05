using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class AppUserDomainServiceTests
{
    [Fact]
    public void Requires_user_name_and_display_name()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AppUserDomainService.EnsureCanSave(new AppUser { UserName = "a", DisplayName = " " }, passwordRequired: false));
        Assert.Contains("Tên hiển thị", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Requires_password_when_creating()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AppUserDomainService.EnsureCanSave(
                new AppUser { UserName = "ketoan", DisplayName = "Kế toán" },
                passwordRequired: true));
        Assert.Contains("Mật khẩu", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Accepts_update_without_password()
    {
        AppUserDomainService.EnsureCanSave(
            new AppUser { Id = 2, UserName = "ketoan", DisplayName = "Kế toán" },
            passwordRequired: false);
    }
}
