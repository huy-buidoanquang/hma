using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class AppUserDomainService
{
    public static void EnsureCanSave(AppUser user, bool passwordRequired)
    {
        if (string.IsNullOrWhiteSpace(user.UserName))
            throw new InvalidOperationException("Tên đăng nhập là bắt buộc.");
        if (string.IsNullOrWhiteSpace(user.DisplayName))
            throw new InvalidOperationException("Tên hiển thị là bắt buộc.");
        if (passwordRequired)
            throw new InvalidOperationException("Mật khẩu là bắt buộc khi tạo user.");
    }
}
