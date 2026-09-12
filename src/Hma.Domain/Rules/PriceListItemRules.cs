using Hma.Domain.Entities;

namespace Hma.Domain.Rules;

public static class PriceListItemRules
{
    public static void EnsureCanSave(PriceListItem item)
    {
        if (item.RouteId is null or <= 0 && item.DeliveryLocationId is null or <= 0)
            throw new InvalidOperationException("Cần chọn tuyến hoặc điểm đến.");
        if (item.RouteId is > 0 && item.DeliveryLocationId is > 0)
            throw new InvalidOperationException("Dòng giá chỉ được áp dụng theo tuyến hoặc theo điểm đến, không chọn đồng thời.");
        if (item.VehicleTypeId == 0)
            throw new InvalidOperationException("Cần chọn loại xe.");
        MoneyRules.EnsureNonNegative(item.UnitPrice, "Đơn giá");
        MoneyRules.EnsureNonNegative(item.Surcharge, "Phụ phí");
    }
}
