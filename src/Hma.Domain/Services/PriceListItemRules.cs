using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class PriceListItemRules
{
    public static void EnsureCanSave(PriceListItem item)
    {
        if (item.RouteId <= 0)
            throw new InvalidOperationException("Cần chọn tuyến.");
        if (item.VehicleTypeId == 0)
            throw new InvalidOperationException("Cần chọn loại xe.");
        MoneyRules.EnsureNonNegative(item.UnitPrice, "Đơn giá");
        MoneyRules.EnsureNonNegative(item.Surcharge, "Phụ phí");
    }
}
