using System.Text.RegularExpressions;

namespace Hma.Domain.Rules;

public static class PhoneRules
{
    private static readonly Regex National = new(@"^0(2\d{8,9}|[35789]\d{8})$", RegexOptions.Compiled);

    public static bool IsValid(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true;
        var national = ToNational(phone);
        return national is not null && National.IsMatch(national);
    }

    public static void EnsureOptional(string? phone, string fieldName = "Số điện thoại")
    {
        if (!IsValid(phone))
            throw new InvalidOperationException($"{fieldName} không hợp lệ. Dùng 10 số bắt đầu bằng 0 hoặc +84.");
    }

    public static string? ToNational(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var raw = new string(phone.Where(static c => c is not (' ' or '.' or '-' or '(' or ')')).ToArray());
        if (raw.StartsWith("+84", StringComparison.Ordinal))
            raw = "0" + raw[3..];
        else if (raw.StartsWith("84", StringComparison.Ordinal) && raw.Length is >= 11 and <= 13)
            raw = "0" + raw[2..];

        return raw;
    }
}
