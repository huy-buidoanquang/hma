using System.Globalization;
using System.Text.RegularExpressions;

namespace Hma.Application.Features.Dispatching.Import;

public static class DispatchImportTonnage
{
    private static readonly Regex Number = new(
        @"^(?<n>\d+(?:[.,]\d+)?)\s*(?:t|tấn|tan)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool TryParse(string? value, out decimal tons)
    {
        tons = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var text = value.Trim().TrimEnd('.');
        var match = Number.Match(text);
        if (!match.Success) return false;
        var raw = match.Groups["n"].Value.Replace(',', '.');
        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            return false;
        if (parsed >= 100 && parsed == decimal.Truncate(parsed))
            parsed /= 100;
        if (parsed <= 0 || parsed > 100)
            return false;
        tons = parsed;
        return true;
    }
}
