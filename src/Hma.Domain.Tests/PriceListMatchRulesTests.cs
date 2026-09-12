using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class PriceListMatchRulesTests
{
    private static readonly DateTime AsOf = new(2026, 8, 15);

    [Fact]
    public void Pick_prefers_private_stable_over_private_fluctuation()
    {
        var stable = Item(customerId: 10, fluctuation: false, created: AsOf, unit: 2_430_000);
        var fluct = Item(customerId: 10, fluctuation: true, created: AsOf.AddDays(1), unit: 9);
        var hit = PriceListMatchRules.Pick([fluct, stable], customerId: 10, AsOf);
        Assert.Same(stable, hit);
        Assert.Equal(2_430_000, hit!.UnitPrice);
    }

    [Fact]
    public void Pick_falls_back_to_private_fluctuation_then_public()
    {
        var privateFluct = Item(customerId: 10, fluctuation: true, created: AsOf, unit: 1_100_000);
        var publicStable = Item(customerId: null, fluctuation: false, created: AsOf, unit: 800_000, listCode: "CONG-BO");
        var hit = PriceListMatchRules.Pick([publicStable, privateFluct], customerId: 10, AsOf);
        Assert.Same(privateFluct, hit);
    }

    [Fact]
    public void Pick_uses_public_when_customer_has_no_private_list()
    {
        var publicStable = Item(customerId: null, fluctuation: false, created: AsOf, unit: 800_000, listCode: "CONG-BO");
        var publicFluct = Item(customerId: null, fluctuation: true, created: AsOf, unit: 500_000, listCode: "CONG-BO-BD");
        var hit = PriceListMatchRules.Pick([publicFluct, publicStable], customerId: 99, AsOf);
        Assert.Same(publicStable, hit);
    }

    [Fact]
    public void Pick_skips_list_outside_effective_window()
    {
        var expiredPrivate = Item(customerId: 10, fluctuation: false, created: AsOf, unit: 1, from: AsOf.AddMonths(-2), to: AsOf.AddDays(-1));
        var futurePublic = Item(customerId: null, fluctuation: false, created: AsOf, unit: 2, from: AsOf.AddDays(1), to: null, listCode: "CONG-BO");
        var currentPublic = Item(customerId: null, fluctuation: false, created: AsOf, unit: 777_000, from: AsOf.AddMonths(-1), to: AsOf.AddMonths(1), listCode: "CONG-BO");
        var hit = PriceListMatchRules.Pick([expiredPrivate, futurePublic, currentPublic], customerId: 10, AsOf);
        Assert.Same(currentPublic, hit);
    }

    [Fact]
    public void Pick_newer_revision_wins_inside_same_tier()
    {
        var older = Item(customerId: 10, fluctuation: false, created: AsOf.AddDays(-2), unit: 1);
        var newer = Item(customerId: 10, fluctuation: false, created: AsOf, unit: 9);
        var hit = PriceListMatchRules.Pick([older, newer], customerId: 10, AsOf);
        Assert.Same(newer, hit);
    }

    [Fact]
    public void IsEffective_includes_boundary_dates()
    {
        var list = new PriceList { EffectiveFrom = AsOf, EffectiveTo = AsOf };
        Assert.True(PriceListMatchRules.IsEffective(list, AsOf));
        Assert.False(PriceListMatchRules.IsEffective(list, AsOf.AddDays(1)));
        Assert.False(PriceListMatchRules.IsEffective(list, AsOf.AddDays(-1)));
    }

    [Fact]
    public void ToQuote_labels_published_versus_private()
    {
        var published = Item(customerId: null, fluctuation: false, created: AsOf, unit: 2_430_000, listCode: "CB");
        published.Route = new Route { Name = "Hà Nội → Hải Phòng" };
        published.VehicleType = new VehicleType { Name = "Xe 5 tấn" };
        var pubQuote = PriceListMatchRules.ToQuote(published);
        Assert.Equal("CB", pubQuote.PriceListCode);
        Assert.Contains("Giá công bố", pubQuote.SourceLabel, StringComparison.Ordinal);
        Assert.Contains("Hà Nội → Hải Phòng", pubQuote.SourceLabel, StringComparison.Ordinal);
        Assert.Equal(2_430_000, pubQuote.UnitPrice);

        var privateItem = Item(customerId: 10, fluctuation: true, created: AsOf, unit: 3_000_000);
        privateItem.PriceListRevision!.PriceList!.Customer = new Customer { Code = "KH001" };
        privateItem.Route = new Route { Name = "Hà Nội → Hải Phòng" };
        privateItem.VehicleType = new VehicleType { Name = "Xe 5 tấn" };
        var privQuote = PriceListMatchRules.ToQuote(privateItem);
        Assert.Equal("RIENG", privQuote.PriceListCode);
        Assert.Contains("Giá riêng KH001", privQuote.SourceLabel, StringComparison.Ordinal);
        Assert.Contains("biến động", privQuote.SourceLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void Pick_prefers_exact_route_over_destination_fallback()
    {
        var fallback = Item(customerId: null, fluctuation: false, created: AsOf, unit: 900_000);
        fallback.RouteId = null;
        fallback.DeliveryLocationId = 99;
        var exact = Item(customerId: null, fluctuation: false, created: AsOf.AddDays(-1), unit: 800_000);
        exact.RouteId = 42;

        var hit = PriceListMatchRules.Pick([fallback, exact], null, AsOf, exactRouteId: 42);

        Assert.Same(exact, hit);
    }

    private static PriceListItem Item(
        int? customerId,
        bool fluctuation,
        DateTime created,
        decimal unit,
        DateTime? from = null,
        DateTime? to = null,
        string listCode = "RIENG")
    {
        var list = new PriceList
        {
            Code = listCode,
            CustomerId = customerId,
            HasPriceFluctuation = fluctuation,
            EffectiveFrom = from,
            EffectiveTo = to
        };
        return new PriceListItem
        {
            RouteId = 1,
            UnitPrice = unit,
            Surcharge = 0,
            PriceListRevision = new PriceListRevision { PriceList = list, CreatedAt = created }
        };
    }
}
