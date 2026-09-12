using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class BillingPeriodRulesTests
{
    [Fact]
    public void ApplyDefault_uses_pickup_month_when_unset()
    {
        var order = new DispatchOrder { PickupAt = new DateTime(2026, 8, 31) };
        BillingPeriodRules.ApplyDefault(order);
        Assert.Equal(2026, order.BillingYear);
        Assert.Equal(8, order.BillingMonth);
    }

    [Fact]
    public void ApplyDefault_keeps_explicit_period()
    {
        var order = new DispatchOrder
        {
            PickupAt = new DateTime(2026, 8, 31),
            BillingYear = 2026,
            BillingMonth = 9
        };
        BillingPeriodRules.ApplyDefault(order);
        Assert.Equal(2026, order.BillingYear);
        Assert.Equal(9, order.BillingMonth);
    }

    [Fact]
    public void Next_rolls_december_to_january()
    {
        var next = BillingPeriodRules.Next(2026, 12);
        Assert.Equal((2027, 1), next);
        Assert.Equal((2026, 9), BillingPeriodRules.Next(2026, 8));
    }

    [Fact]
    public void EnsureValid_rejects_month_out_of_range()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BillingPeriodRules.EnsureValid(2026, 13));
        Assert.Contains("Tháng kỳ kế toán", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyDefault_then_next_moves_unbilled_order_without_touching_pickup()
    {
        var pickup = new DateTime(2026, 12, 20, 8, 0, 0);
        var order = new DispatchOrder { PickupAt = pickup };
        BillingPeriodRules.ApplyDefault(order);
        var next = BillingPeriodRules.Next(order.BillingYear, order.BillingMonth);
        order.BillingYear = next.Year;
        order.BillingMonth = next.Month;
        Assert.Equal(pickup, order.PickupAt);
        Assert.Equal(2027, order.BillingYear);
        Assert.Equal(1, order.BillingMonth);
    }
}
