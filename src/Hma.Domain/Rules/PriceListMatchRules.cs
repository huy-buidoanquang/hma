using Hma.Domain.Models;
using Hma.Domain.Entities;

namespace Hma.Domain.Rules;

public static class PriceListMatchRules
{
    public static bool IsEffective(PriceList list, DateTime asOf)
    {
        if (!list.IsLocked) return false;
        var day = asOf.Date;
        if (list.EffectiveFrom is { } from && from.Date > day) return false;
        if (list.EffectiveTo is { } to && to.Date < day) return false;
        return true;
    }

    public static PriceListItem? Pick(
        IEnumerable<PriceListItem> items,
        int? customerId,
        DateTime asOf,
        int? exactRouteId = null)
    {
        var effective = items.Where(i =>
            i.PriceListRevision?.PriceList is { } list && IsEffective(list, asOf)).ToList();

        PriceListItem? Match(int? wantedCustomer, bool? fluctuation)
        {
            var subset = effective.Where(i => i.PriceListRevision!.PriceList!.CustomerId == wantedCustomer);
            if (fluctuation is { } flag)
                subset = subset.Where(i => i.PriceListRevision!.PriceList!.HasPriceFluctuation == flag);
            return subset
                .OrderByDescending(i => exactRouteId is not null && i.RouteId == exactRouteId)
                .ThenByDescending(i => i.PriceListRevision!.CreatedAt)
                .FirstOrDefault();
        }

        return Match(customerId, false)
               ?? Match(customerId, true)
               ?? Match(null, false)
               ?? Match(null, true);
    }

    public static FreightQuote ToQuote(PriceListItem hit, PriceListFluctuation? fluctuation = null)
    {
        var list = hit.PriceListRevision?.PriceList;
        var kind = list?.CustomerId is null
            ? "Giá công bố"
            : $"Giá riêng {list.Customer?.Code ?? list.Code}";
        if (list?.HasPriceFluctuation == true)
            kind += " · bảng đặc biệt";
        var route = hit.Route?.Name
                    ?? (hit.DeliveryLocation is null ? "" : $"đến {hit.DeliveryLocation.Name}");
        var vehicle = hit.VehicleType?.Name ?? "";
        var fluctuationAmount = fluctuation is null
            ? 0
            : PriceFluctuationRules.CalculateAmount(hit.UnitPrice, fluctuation);
        var fluctuationLabel = fluctuation is null
            ? ""
            : fluctuation.Type == Hma.Domain.Enums.PriceFluctuationType.Percentage
                ? $" · biến động {fluctuation.Value:+0.####;-0.####}% ({fluctuationAmount:+#,##0.##;-#,##0.##})"
                : $" · biến động {fluctuationAmount:+#,##0.##;-#,##0.##}";
        return new FreightQuote
        {
            PriceListItemId = hit.Id,
            PriceListFluctuationId = fluctuation?.Id,
            PriceListCode = list?.Code ?? "",
            BaseUnitPrice = hit.UnitPrice,
            FluctuationAmount = fluctuationAmount,
            UnitPrice = hit.UnitPrice + fluctuationAmount,
            Surcharge = hit.Surcharge,
            SourceLabel = $"{kind} · {route} · {vehicle} · gốc {hit.UnitPrice:N0}{fluctuationLabel} · cuối {hit.UnitPrice + fluctuationAmount:N0}"
        };
    }
}
