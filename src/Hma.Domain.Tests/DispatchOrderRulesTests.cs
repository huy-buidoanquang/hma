using Hma.Domain.Entities;
using Hma.Domain.Services;

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
        var order = new DispatchOrder { UnitPrice = -1 };
        var ex = Assert.Throws<InvalidOperationException>(() => DispatchOrderRules.EnsureCanSave(order));
        Assert.Contains("không được âm", ex.Message, StringComparison.Ordinal);
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
}
