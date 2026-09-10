using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Hma.Domain.Services;

namespace Hma.Application.Services;

public static class DispatchImportCatalogKey
{
    private static readonly Regex WarehousePrefix = new(
        @"^\d+\s*kho\s+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex InspectionPrefix = new(
        @"^(kiểm|kiẻm)\s+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex[] Tails =
    [
        new(@"[,;\s+]+\d+\s*k\s+chờ(\s+lấy hàng)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+chờ\s+\d+\s*(k|giơf|giờ)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+\d+\s*k\s+lưu đêm$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+lưu(?:\s+ca)?\s+đêm(?:\s+NBA)?(?:\s*[+]?\s*\d+\s*kho)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+lưu\s+nhà máy$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+lưu\s+xe$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+lạnh(\s*,\s*bốc)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+vé(\s+ct)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+(kiểm|kiẻm)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+cao tốc$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+ko hàng$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+đeem$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+đêm$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+bốc(\s+\d+\s*k)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+chèn\s+\d+\s*k$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+luật\s+\d+\s*k$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+rung$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+\d+\s*điểm$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[,;\s+]+\d+\s*kho$", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    public static string FoldKey(string? value)
    {
        var text = TrimToken(value);
        if (text.Length == 0)
            return "";

        text = text.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsWhiteSpace(ch))
                continue;
            sb.Append(char.ToUpperInvariant(ch));
        }

        return sb.ToString();
    }

    public static string CanonicalLocationToken(string? token)
    {
        var original = TrimToken(token);
        if (original.Length == 0)
            return "";

        var current = original;
        for (var i = 0; i < 16; i++)
        {
            var next = WarehousePrefix.Replace(current, "", 1);
            if (next.Length == current.Length)
                next = InspectionPrefix.Replace(current, "", 1);
            if (next.Length == current.Length)
            {
                foreach (var tail in Tails)
                {
                    next = tail.Replace(current, "", 1);
                    if (next.Length != current.Length)
                        break;
                }
            }

            next = next.Trim().TrimEnd(',', ';', '+').Trim();
            if (next == current)
                break;
            if (next.Length == 0)
                return original;
            current = next;
        }

        return current;
    }

    public static string TrimToken(string? value) =>
        AliasText.Normalize(value).TrimStart(':').TrimEnd('\\').Trim();
}
