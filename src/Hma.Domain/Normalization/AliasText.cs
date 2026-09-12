namespace Hma.Domain.Normalization;

public static class AliasText
{
    public const int MaxLength = 100;

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        var parts = value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }

    public static bool EqualsNormalized(string? left, string? right) =>
        Normalize(left).Equals(Normalize(right), StringComparison.OrdinalIgnoreCase);
}
