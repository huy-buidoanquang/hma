using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class LocationAliasRules
{
    public static void EnsureCanSave(string? alias, int locationId, LocationAliasKind kind)
    {
        var text = AliasText.Normalize(alias);
        if (text.Length == 0)
            throw new InvalidOperationException("Bí danh điểm là bắt buộc.");
        if (text.Length > AliasText.MaxLength)
            throw new InvalidOperationException($"Bí danh tối đa {AliasText.MaxLength} ký tự.");
        if (locationId <= 0)
            throw new InvalidOperationException("Cần chọn điểm đích.");
        if (!Enum.IsDefined(kind))
            throw new InvalidOperationException("Phạm vi bí danh không hợp lệ.");
    }

    public static void EnsureUnique(string alias, IEnumerable<string> otherAliases)
    {
        AliasDictionaryRules.EnsureUniqueKey(alias, otherAliases);
    }

    public static bool AppliesToPickup(LocationAliasKind kind) =>
        kind is LocationAliasKind.Both or LocationAliasKind.Pickup;

    public static bool AppliesToDelivery(LocationAliasKind kind) =>
        kind is LocationAliasKind.Both or LocationAliasKind.Delivery;

    public static bool AppliesToVia(LocationAliasKind kind) =>
        kind is LocationAliasKind.Both;
}
