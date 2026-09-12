using Hma.Domain.Entities;
using Hma.Domain.Enums;

namespace Hma.Domain.Rules;

public static class PriceFluctuationRules
{
    public static void EnsureCanSave(PriceListFluctuation fluctuation)
    {
        if (fluctuation.PriceListId <= 0)
            throw new InvalidOperationException("Không tìm thấy bảng giá áp dụng biến động.");
        if (!Enum.IsDefined(fluctuation.Type))
            throw new InvalidOperationException("Loại biến động giá không hợp lệ.");
        if (fluctuation.Value == 0)
            throw new InvalidOperationException("Biến động giá phải khác 0.");
        if (fluctuation.Type == PriceFluctuationType.Percentage)
        {
            if (fluctuation.Value < -100)
                throw new InvalidOperationException("Mức giảm theo phần trăm không được nhỏ hơn -100%.");
            if (decimal.Round(fluctuation.Value, 4) != fluctuation.Value)
                throw new InvalidOperationException("Biến động phần trăm chỉ được tối đa 4 chữ số thập phân.");
        }
        else
        {
            if (fluctuation.Value is < -MoneyRules.MaxAmount or > MoneyRules.MaxAmount)
                throw new InvalidOperationException("Biến động cố định vượt quá giới hạn cho phép.");
            if (decimal.Round(fluctuation.Value, 2) != fluctuation.Value)
                throw new InvalidOperationException("Biến động cố định chỉ được tối đa 2 chữ số thập phân.");
        }
        if (fluctuation.EffectiveTo is { } to && to.Date < fluctuation.EffectiveFrom.Date)
            throw new InvalidOperationException("Ngày kết thúc biến động phải sau hoặc bằng ngày bắt đầu.");
        if (string.IsNullOrWhiteSpace(fluctuation.Reason))
            throw new InvalidOperationException("Cần nhập lý do biến động giá.");
    }

    public static void EnsureCanManage(PriceList priceList)
    {
        if (!priceList.IsLocked)
            throw new InvalidOperationException("Chỉ được điều chỉnh biến động sau khi bảng giá đã khóa.");
    }

    public static bool IsEffective(PriceListFluctuation fluctuation, DateTime asOf)
    {
        var day = asOf.Date;
        return fluctuation.EffectiveFrom.Date <= day
               && (fluctuation.EffectiveTo is null || fluctuation.EffectiveTo.Value.Date >= day);
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

    public static decimal CalculateAmount(decimal unitPrice, PriceListFluctuation fluctuation)
    {
        MoneyRules.EnsureNonNegative(unitPrice, "Đơn giá gốc");
        var amount = fluctuation.Type switch
        {
            PriceFluctuationType.Percentage => decimal.Round(
                unitPrice * fluctuation.Value / 100m,
                2,
                MidpointRounding.AwayFromZero),
            PriceFluctuationType.FixedAmount => decimal.Round(
                fluctuation.Value,
                2,
                MidpointRounding.AwayFromZero),
            _ => throw new InvalidOperationException("Loại biến động giá không hợp lệ."),
        };

        if (unitPrice + amount < 0)
            throw new InvalidOperationException("Biến động làm đơn giá cuối nhỏ hơn 0.");
        MoneyRules.EnsureNonNegative(unitPrice + amount, "Đơn giá sau biến động");
        return amount;
    }

    public static PriceListFluctuation? Pick(
        IEnumerable<PriceListFluctuation> fluctuations,
        DateTime asOf) =>
        fluctuations
            .Where(x => IsEffective(x, asOf))
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefault();
}
