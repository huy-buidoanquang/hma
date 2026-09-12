using Hma.Application.Common.Formatting;
using Hma.Domain.Entities;

namespace Hma.Application.Tests;

public class AmountTextTests
{
    [Fact]
    public void From_delegates_to_vietnamese_words() =>
        Assert.Equal("một trăm đồng", AmountText.From(100));

    [Fact]
    public void Refresh_uses_total_after_approved_exception()
    {
        var order = new DispatchOrder
        {
            UnitPrice = 1_000_000,
            AmountInWords = AmountText.From(1_000_000)
        };
        order.RecalculateTotal();
        order.ApplyApprovedException(50_000, 0);

        AmountText.Refresh(order);

        Assert.Equal(AmountText.From(1_050_000), order.AmountInWords);
        Assert.NotEqual(AmountText.From(1_000_000), order.AmountInWords);
    }
}
