using System.Globalization;

namespace Hma.Domain.Rules;

public static class MoneyRules
{
    public const decimal MaxAmount = 999_999_999_999m;
    private static readonly CultureInfo CommaGroup = CultureInfo.GetCultureInfo("en-US");

    public static string Format(decimal amount) => amount.ToString("#,##0", CommaGroup);

    public static bool TryParse(string? text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var s = StripCurrency(text);
        if (s.Length == 0) return false;

        var lastComma = s.LastIndexOf(',');
        var lastDot = s.LastIndexOf('.');
        if (lastComma >= 0 && lastDot >= 0)
        {
            var last = Math.Max(lastComma, lastDot);
            var decimals = s.Length - last - 1;
            if (decimals is 1 or 2)
            {
                var thousand = lastComma > lastDot ? '.' : ',';
                s = s.Replace(thousand.ToString(), "", StringComparison.Ordinal);
                s = s.Replace(',', '.');
                return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
            }

            s = s.Replace(",", "", StringComparison.Ordinal).Replace(".", "", StringComparison.Ordinal);
            return decimal.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out amount);
        }

        if (lastComma >= 0)
        {
            var decimals = s.Length - lastComma - 1;
            if (decimals is 1 or 2 && s.Count(static c => c == ',') == 1)
            {
                s = s.Replace(',', '.');
                return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
            }

            s = s.Replace(",", "", StringComparison.Ordinal);
            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }

        if (lastDot >= 0)
        {
            var decimals = s.Length - lastDot - 1;
            if (decimals is 1 or 2 && s.Count(static c => c == '.') == 1)
                return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

            s = s.Replace(".", "", StringComparison.Ordinal);
            return decimal.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out amount);
        }

        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    public static void EnsureNonNegative(decimal amount, string fieldName)
    {
        if (amount < 0)
            throw new InvalidOperationException($"{fieldName} không được âm.");
        if (amount > MaxAmount)
            throw new InvalidOperationException($"{fieldName} vượt quá giới hạn cho phép.");
        if (decimal.Round(amount, 2) != amount)
            throw new InvalidOperationException($"{fieldName} chỉ được tối đa 2 chữ số thập phân.");
    }

    public static void EnsurePositive(decimal amount, string fieldName)
    {
        EnsureNonNegative(amount, fieldName);
        if (amount <= 0)
            throw new InvalidOperationException($"{fieldName} phải lớn hơn 0.");
    }

    private static string StripCurrency(string text)
    {
        var s = text.Trim();
        string[] tokens = ["VNĐ", "VND", "vnđ", "vnd", "đồng", "₫"];
        foreach (var token in tokens)
            s = s.Replace(token, "", StringComparison.OrdinalIgnoreCase);
        return s.Replace("đ", "", StringComparison.OrdinalIgnoreCase).Trim();
    }
}
