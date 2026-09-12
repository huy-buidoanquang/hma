using System.Text.RegularExpressions;

namespace Hma.Domain.Rules;

public static class TaxCodeRules
{
    private static readonly Regex Pattern = new(@"^\d{10}(-\d{3})?$", RegexOptions.Compiled);

    public static bool IsValid(string? taxCode)
    {
        if (string.IsNullOrWhiteSpace(taxCode)) return true;
        var value = taxCode.Trim().Replace(" ", "", StringComparison.Ordinal);
        if (value.Length == 13 && value.All(char.IsDigit))
            value = $"{value[..10]}-{value[10..]}";
        return Pattern.IsMatch(value);
    }

    public static void EnsureOptional(string? taxCode, string fieldName = "Mã số thuế")
    {
        if (!IsValid(taxCode))
            throw new InvalidOperationException($"{fieldName} không hợp lệ. Dùng 10 số hoặc 10 số kèm 3 số chi nhánh.");
    }
}
