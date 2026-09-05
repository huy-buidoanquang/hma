using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class RouteAliasRulesTests
{
    [Fact]
    public void Rejects_blank_alias_or_route()
    {
        var blank = Assert.Throws<InvalidOperationException>(() => RouteAliasRules.EnsureCanSave(" ", 1));
        Assert.Contains("bắt buộc", blank.Message, StringComparison.Ordinal);
        var route = Assert.Throws<InvalidOperationException>(() => RouteAliasRules.EnsureCanSave("nb-hp", 0));
        Assert.Contains("tuyến đích", route.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Accepts_normalized_alias()
    {
        RouteAliasRules.EnsureCanSave("nb - hp", 4);
    }
}
