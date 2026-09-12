using System.Text.RegularExpressions;

namespace Hma.Application.Features.Dispatching.Import;

public static class DispatchImportRoute
{
    private static readonly string[] Separators =
        [" =- ", "=-", " - ", " – ", " — ", " = ", "-"];

    private static readonly Regex DateCaption = new(
        @"^Ngày\s+\d{1,2}\s+tháng\s+\d{1,2}\s+năm\s+\d{4}$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool IsIdle(string? route)
    {
        if (string.IsNullOrWhiteSpace(route)) return false;
        var key = route.Trim();
        return key.Equals("x", StringComparison.OrdinalIgnoreCase)
               || key.Equals("lưu", StringComparison.OrdinalIgnoreCase)
               || key.Equals("luu", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDateCaption(string? route) =>
        !string.IsNullOrWhiteSpace(route) && DateCaption.IsMatch(route.Trim());

    public static bool TrySplit(string? route, out string pickup, out string delivery)
    {
        pickup = "";
        delivery = "";
        if (!TrySplitStops(route, out var stops) || stops.Length != 2)
            return false;
        pickup = stops[0];
        delivery = stops[1];
        return true;
    }

    public static bool TrySplitStops(string? route, out string[] stops)
    {
        stops = [];
        if (string.IsNullOrWhiteSpace(route) || IsIdle(route) || IsDateCaption(route))
            return false;
        var text = route.Trim();
        var parts = new List<string>();
        while (text.Length > 0)
        {
            var bestAt = -1;
            string? bestSep = null;
            foreach (var sep in Separators)
            {
                var at = text.IndexOf(sep, StringComparison.Ordinal);
                if (at <= 0)
                    continue;
                if (bestAt < 0 || at < bestAt)
                {
                    bestAt = at;
                    bestSep = sep;
                }
            }

            if (bestAt < 0 || bestSep is null)
            {
                parts.Add(text.Trim());
                break;
            }

            var left = text[..bestAt].Trim();
            if (left.Length == 0)
                return false;
            parts.Add(left);
            text = text[(bestAt + bestSep.Length)..].Trim();
            if (text.Length == 0)
                return false;
        }

        if (parts.Count < 2)
            return false;
        stops = [.. parts];
        return true;
    }
}
