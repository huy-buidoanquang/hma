using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class PartnerRateRules
{
    public static void EnsureCanSave(PartnerRate rate)
    {
        if (rate.PartnerId <= 0)
            throw new InvalidOperationException("Cần chọn đối tác vận tải.");
        if (rate.RouteId <= 0)
            throw new InvalidOperationException("Cần chọn tuyến.");
        if (rate.VehicleTypeId <= 0)
            throw new InvalidOperationException("Cần chọn loại xe.");
        if (rate.EffectiveTo is { } to && to.Date < rate.EffectiveFrom.Date)
            throw new InvalidOperationException("Ngày kết thúc hiệu lực phải sau hoặc bằng ngày bắt đầu.");
        MoneyRules.EnsureNonNegative(rate.UnitPrice, "Giá mua");
        MoneyRules.EnsureNonNegative(rate.Surcharge, "Phụ phí mua");
    }

    public static bool PeriodsOverlap(
        DateTime firstFrom,
        DateTime? firstTo,
        DateTime secondFrom,
        DateTime? secondTo)
    {
        var firstEnd = firstTo?.Date ?? DateTime.MaxValue.Date;
        var secondEnd = secondTo?.Date ?? DateTime.MaxValue.Date;
        return firstFrom.Date <= secondEnd && secondFrom.Date <= firstEnd;
    }
}
