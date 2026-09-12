using Hma.Domain.Entities;

namespace Hma.Domain.Rules;

public static class PriceListRules
{
    public static void EnsureCanSave(PriceList list)
    {
        CatalogItemRules.EnsureCanSave(list.Code, list.Name, "bảng giá");
        if (list.EffectiveFrom is { } from && list.EffectiveTo is { } to && to.Date < from.Date)
            throw new InvalidOperationException("Ngày hiệu lực đến phải sau hoặc bằng ngày bắt đầu.");
    }

    public static void EnsureCanModify(PriceList list)
    {
        if (list.IsLocked)
            throw new InvalidOperationException("Bảng giá đã khóa nên không thể sửa dòng giá hoặc phiên bản.");
    }

    public static void EnsureCanLock(PriceList list)
    {
        if (list.IsLocked)
            throw new InvalidOperationException("Bảng giá đã được khóa.");
        if (string.IsNullOrWhiteSpace(list.LockReason))
            throw new InvalidOperationException("Cần nhập lý do khóa bảng giá.");
    }
}
