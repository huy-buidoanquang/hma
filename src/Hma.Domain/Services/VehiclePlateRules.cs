using System.Text.RegularExpressions;

namespace Hma.Domain.Services;

public static class VehiclePlateRules
{
    public const string FormatMessage =
        "Biển số phải dạng 29C-23456 hoặc 30H-4567 (hai số, một chữ, gạch ngang, 4 hoặc 5 số).";

    private static readonly Regex Compact = new(
        @"^(?<prov>\d{2})(?<letter>[A-Za-z])(?<serial>\d{4,5})$",
        RegexOptions.CultureInvariant);

    public static string Canonicalize(string? value)
    {
        if (!TryCanonicalize(value, out var canonical))
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Biển số xe là bắt buộc.");
            throw new InvalidOperationException(FormatMessage);
        }

        return canonical;
    }

    public static bool TryCanonicalize(string? value, out string canonical)
    {
        canonical = "";
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var compact = value.Trim().Replace(" ", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal);
        var match = Compact.Match(compact);
        if (!match.Success)
            return false;
        var letter = char.ToUpperInvariant(match.Groups["letter"].Value[0]);
        canonical = $"{match.Groups["prov"].Value}{letter}-{match.Groups["serial"].Value}";
        return true;
    }

    public static IReadOnlyList<string> DictionaryForms(string canonical)
    {
        if (!TryCanonicalize(canonical, out var plate))
            return [];
        var compact = plate.Replace("-", "", StringComparison.Ordinal);
        var lower = compact[..2] + char.ToLowerInvariant(compact[2]) + compact[3..];
        return [compact, lower];
    }
}
