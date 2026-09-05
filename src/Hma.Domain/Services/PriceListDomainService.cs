using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class PriceListDomainService
{
    public static void EnsureCanSave(PriceList list)
    {
        CatalogItemDomainService.EnsureCanSave(list.Code, list.Name, "bảng giá");
        if (list.EffectiveFrom is { } from && list.EffectiveTo is { } to && to.Date < from.Date)
            throw new InvalidOperationException("Ngày hiệu lực đến phải sau hoặc bằng ngày bắt đầu.");
    }
}
