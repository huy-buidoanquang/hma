using Hma.Domain.Models;
using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class FreightPricingRulesTests
{
    [Fact]
    public void Matching_quote_captures_source_and_clears_manual_override()
    {
        var order = new DispatchOrder
        {
            UnitPrice = 1_000,
            Surcharge = 100,
            IsFreightManual = true,
            FreightOverrideReason = "old"
        };
        var quote = new FreightQuote
        {
            PriceListItemId = 9,
            PriceListFluctuationId = 7,
            UnitPrice = 1_000,
            Surcharge = 100,
            SourceLabel = "BG-01"
        };

        FreightPricingRules.CaptureSource(order, quote);

        Assert.False(order.IsFreightManual);
        Assert.Equal(9, order.PriceListItemId);
        Assert.Equal(7, order.PriceListFluctuationId);
        Assert.Equal("BG-01", order.PriceSourceSnapshot);
        Assert.Null(order.FreightOverrideReason);
    }

    [Fact]
    public void Manual_price_requires_reason()
    {
        var order = new DispatchOrder { UnitPrice = 900, Surcharge = 100 };
        var quote = new FreightQuote { PriceListItemId = 9, UnitPrice = 1_000, Surcharge = 100 };

        Assert.Throws<InvalidOperationException>(() =>
            FreightPricingRules.CaptureSource(order, quote));

        order.FreightOverrideReason = "Khách xác nhận giá riêng";
        FreightPricingRules.CaptureSource(order, quote);
        Assert.True(order.IsFreightManual);
        Assert.Equal("Khách xác nhận giá riêng", order.FreightOverrideReason);
    }

    [Fact]
    public void Missing_quote_requires_manual_reason()
    {
        var order = new DispatchOrder { UnitPrice = 900 };

        Assert.Throws<InvalidOperationException>(() => FreightPricingRules.CaptureSource(order, null));
    }
}
