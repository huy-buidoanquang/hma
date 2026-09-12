using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class DispatchOrderRulesTests
{
    [Fact]
    public void RecalculateTotal_sums_freight_parts()
    {
        var order = new DispatchOrder { UnitPrice = 1_000_000, Surcharge = 50_000, ExtraCost = 20_000 };
        order.RecalculateTotal();
        Assert.Equal(1_070_000, order.TotalAmount);
    }

    [Fact]
    public void EnsureCanSave_rejects_negative_freight()
    {
        var order = ValidOrder();
        order.UnitPrice = -1;
        var ex = Assert.Throws<InvalidOperationException>(() => DispatchOrderRules.EnsureCanSave(order));
        Assert.Contains("không được âm", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureCanSave_requires_customer_and_route()
    {
        var order = new DispatchOrder { PickupAt = new DateTime(2026, 8, 31) };
        var ex = Assert.Throws<InvalidOperationException>(() => DispatchOrderRules.EnsureCanSave(order));
        Assert.Contains("khách hàng", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureCanSave_requires_route_and_stops()
    {
        var order = ValidOrder();
        order.RouteId = null;
        var ex = Assert.Throws<InvalidOperationException>(() => DispatchOrderRules.EnsureCanSave(order));
        Assert.Contains("tuyến", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RouteLabel_joins_stop_snapshots()
    {
        var order = new DispatchOrder();
        order.ReplaceStops([(1, "Nội Bài"), (2, "Hải Phòng"), (3, "Hạ Long")]);
        Assert.Equal("Nội Bài → Hải Phòng → Hạ Long", order.RouteLabel);
        Assert.Equal("Nội Bài", order.PickupLocationName);
        Assert.Equal("Hạ Long", order.DeliveryLocationName);
    }

    [Fact]
    public void Locked_order_cannot_edit()
    {
        var order = new DispatchOrder { Status = DispatchStatus.Locked };
        Assert.False(order.CanEdit);
    }

    [Fact]
    public void Deleted_order_cannot_edit()
    {
        var order = new DispatchOrder { IsDeleted = true };
        Assert.False(order.CanEdit);
    }

    private static DispatchOrder ValidOrder()
    {
        var order = new DispatchOrder
        {
            CustomerId = 1,
            RouteId = 1,
            VehicleId = 1,
            DriverId = 1,
            UnitPrice = 1,
            PickupAt = new DateTime(2026, 8, 31)
        };
        order.ReplaceStops([(1, "A"), (2, "B")]);
        return order;
    }
}
