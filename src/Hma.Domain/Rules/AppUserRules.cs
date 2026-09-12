using Hma.Domain.Entities;

namespace Hma.Domain.Rules;

public static class AppUserRules
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

    public static void EnsurePasswordIsStrong(string password)
    {
        if (password.Length < 12
            || !password.Any(char.IsUpper)
            || !password.Any(char.IsLower)
            || !password.Any(char.IsDigit)
            || !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw new InvalidOperationException(
                "Mật khẩu phải có ít nhất 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
        }
    }
}
