using System.Text.RegularExpressions;

namespace Hma.Domain.Rules;

public static class EmailRules
{
    private static readonly Regex Pattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsValid(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return true;
        var value = email.Trim();
        if (value.Length > 254) return false;
        if (value.Contains("..", StringComparison.Ordinal)) return false;
        return Pattern.IsMatch(value);
    }

    public static void EnsureOptional(string? email, string fieldName = "Email")
    {
        if (!IsValid(email))
            throw new InvalidOperationException($"{fieldName} không hợp lệ.");
    }
}
