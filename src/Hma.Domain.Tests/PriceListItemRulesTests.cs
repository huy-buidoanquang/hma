using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class PriceListItemRulesTests
{
    [Fact]
    public void Requires_route_and_vehicle_type()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PriceListItemRules.EnsureCanSave(new PriceListItem { VehicleTypeId = 1, UnitPrice = 1 }));
        Assert.Contains("tuyến", ex.Message, StringComparison.OrdinalIgnoreCase);

        ex = Assert.Throws<InvalidOperationException>(() =>
            PriceListItemRules.EnsureCanSave(new PriceListItem { RouteId = 1, UnitPrice = 1 }));
        Assert.Contains("loại xe", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Accepts_full_route_item()
    {
        PriceListItemRules.EnsureCanSave(new PriceListItem
        {
            RouteId = 4,
            VehicleTypeId = 2,
            UnitPrice = 1_000_000,
            Surcharge = 0
        });
    }
}
