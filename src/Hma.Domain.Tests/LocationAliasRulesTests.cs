using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class LocationAliasRulesTests
{
    [Fact]
    public void Rejects_blank_alias_or_location()
    {
        var blank = Assert.Throws<InvalidOperationException>(() =>
            LocationAliasRules.EnsureCanSave("  ", 1, LocationAliasKind.Both));
        Assert.Contains("bắt buộc", blank.Message, StringComparison.Ordinal);

        var location = Assert.Throws<InvalidOperationException>(() =>
            LocationAliasRules.EnsureCanSave("nb", 0, LocationAliasKind.Both));
        Assert.Contains("điểm đích", location.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_too_long_alias()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            LocationAliasRules.EnsureCanSave(new string('a', AliasText.MaxLength + 1), 1, LocationAliasKind.Both));
        Assert.Contains("tối đa", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Accepts_normalized_alias()
    {
        LocationAliasRules.EnsureCanSave("nb", 3, LocationAliasKind.Pickup);
    }

    [Fact]
    public void Unique_is_case_insensitive_across_dictionaries()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            LocationAliasRules.EnsureUnique("NB", ["nb", "qv"]));
        Assert.Contains("đã tồn tại", ex.Message, StringComparison.Ordinal);
        LocationAliasRules.EnsureUnique("hp", ["nb", "qv"]);
        var cross = Assert.Throws<InvalidOperationException>(() =>
            AliasDictionaryRules.EnsureUniqueKey("hn-hp", ["nb", "HN-HP"]));
        Assert.Contains("đã tồn tại", cross.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_alias_equal_to_location_or_route_catalog()
    {
        var locations = new[] { new Location { Code = "NB", Name = "Nội Bài" } };
        var routes = new[] { new Route { Code = "HN-HP", Name = "Hà Nội → Hải Phòng" } };
        var loc = Assert.Throws<InvalidOperationException>(() =>
            AliasDictionaryRules.EnsureNotCatalog("nb", AliasDictionaryRules.FromLocations(locations), "điểm"));
        Assert.Contains("điểm", loc.Message, StringComparison.Ordinal);
        var route = Assert.Throws<InvalidOperationException>(() =>
            AliasDictionaryRules.EnsureNotCatalog("Hà Nội → Hải Phòng", AliasDictionaryRules.FromRoutes(routes), "tuyến"));
        Assert.Contains("tuyến", route.Message, StringComparison.Ordinal);
        AliasDictionaryRules.EnsureNotCatalog("qv", AliasDictionaryRules.FromLocations(locations), "điểm");
    }

    [Fact]
    public void Kind_applies_to_pickup_delivery_and_via()
    {
        Assert.True(LocationAliasRules.AppliesToPickup(LocationAliasKind.Both));
        Assert.True(LocationAliasRules.AppliesToPickup(LocationAliasKind.Pickup));
        Assert.False(LocationAliasRules.AppliesToPickup(LocationAliasKind.Delivery));
        Assert.True(LocationAliasRules.AppliesToDelivery(LocationAliasKind.Both));
        Assert.True(LocationAliasRules.AppliesToDelivery(LocationAliasKind.Delivery));
        Assert.False(LocationAliasRules.AppliesToDelivery(LocationAliasKind.Pickup));
        Assert.True(LocationAliasRules.AppliesToVia(LocationAliasKind.Both));
        Assert.False(LocationAliasRules.AppliesToVia(LocationAliasKind.Pickup));
    }
}
