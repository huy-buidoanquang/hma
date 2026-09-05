using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class PriceListMatchRules
{
    public static bool IsEffective(PriceList list, DateTime asOf)
    {
        var day = asOf.Date;
        if (list.EffectiveFrom is { } from && from.Date > day) return false;
        if (list.EffectiveTo is { } to && to.Date < day) return false;
        return true;
    }

    public static PriceListItem? Pick(
        IEnumerable<PriceListItem> items,
        int? customerId,
        DateTime asOf)
    {
        var effective = items.Where(i =>
            i.PriceListRevision?.PriceList is { } list && IsEffective(list, asOf)).ToList();

        PriceListItem? Match(int? wantedCustomer, bool? fluctuation)
        {
            var subset = effective.Where(i => i.PriceListRevision!.PriceList!.CustomerId == wantedCustomer);
            if (fluctuation is { } flag)
                subset = subset.Where(i => i.PriceListRevision!.PriceList!.HasPriceFluctuation == flag);
            return subset
                .OrderByDescending(i => i.PriceListRevision!.CreatedAt)
                .FirstOrDefault();
        }

        return Match(customerId, false)
               ?? Match(customerId, true)
               ?? Match(null, false)
               ?? Match(null, true);
    }

    public static FreightQuote ToQuote(PriceListItem hit)
    {
        var list = hit.PriceListRevision?.PriceList;
        var kind = list?.CustomerId is null
            ? "Giá công bố"
            : $"Giá riêng {list.Customer?.Code ?? list.Code}";
        if (list?.HasPriceFluctuation == true)
            kind += " · biến động";
        var route = hit.Route?.Name ?? "";
        var vehicle = hit.VehicleType?.Name ?? "";
        return new FreightQuote
        {
            UnitPrice = hit.UnitPrice,
            Surcharge = hit.Surcharge,
            SourceLabel = $"{kind} · {route} · {vehicle} · {hit.UnitPrice:N0}"
        };
    }
}
