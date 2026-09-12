using Hma.Domain.Entities;

namespace Hma.Domain.Rules;

public static class BillingPeriodRules
{
    public static void EnsureValid(int year, int month)
    {
        if (year < 2000 || year > 2100)
            throw new InvalidOperationException("Năm kỳ kế toán không hợp lệ.");
        if (month is < 1 or > 12)
            throw new InvalidOperationException("Tháng kỳ kế toán phải từ 1 đến 12.");
    }

    public static void ApplyDefault(DispatchOrder order)
    {
        if (order.BillingYear is 0 || order.BillingMonth is 0)
        {
            order.BillingYear = order.PickupAt.Year;
            order.BillingMonth = order.PickupAt.Month;
        }
        EnsureValid(order.BillingYear, order.BillingMonth);
    }

    public static (int Year, int Month) Next(int year, int month)
    {
        EnsureValid(year, month);
        return month == 12 ? (year + 1, 1) : (year, month + 1);
    }
}
