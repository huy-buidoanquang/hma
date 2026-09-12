using Hma.Domain.Models;
using Hma.Domain.Entities;

namespace Hma.Domain.Rules;

public static class FreightPricingRules
{
    public static void CaptureSource(DispatchOrder order, FreightQuote? quote)
    {
        var matchesQuote = quote is not null
                           && order.UnitPrice == quote.UnitPrice
                           && order.Surcharge == quote.Surcharge;
        if (matchesQuote)
        {
            order.PriceListItemId = quote!.PriceListItemId;
            order.PriceSourceSnapshot = quote.SourceLabel;
            order.IsFreightManual = false;
            order.FreightOverrideReason = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(order.FreightOverrideReason))
            throw new InvalidOperationException("Cần nhập lý do khi dùng cước thủ công hoặc khác bảng giá.");

        order.PriceListItemId = quote?.PriceListItemId;
        order.PriceSourceSnapshot = quote?.SourceLabel;
        order.IsFreightManual = true;
        order.FreightOverrideReason = order.FreightOverrideReason.Trim();
    }
}
