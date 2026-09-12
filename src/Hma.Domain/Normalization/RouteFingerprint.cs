namespace Hma.Domain.Normalization;

public static class RouteFingerprint
{
    public static string From(IEnumerable<int> locationIds)
    {
        var ids = locationIds.ToList();
        if (ids.Count < 2)
            throw new InvalidOperationException("Tuyến cần ít nhất hai điểm.");
        if (ids.Any(id => id <= 0))
            throw new InvalidOperationException("Mỗi điểm trên tuyến phải được chọn.");
        return string.Join("-", ids);
    }

    public static string FormatLabel(IEnumerable<string?> names)
    {
        var parts = names
            .Select(n => (n ?? "").Trim())
            .Where(n => n.Length > 0)
            .ToList();
        return string.Join(" → ", parts);
    }
}
