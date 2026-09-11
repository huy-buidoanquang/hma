using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class DispatchKpiRules
{
    public static bool IsEarnedTrip(DispatchOrder order) =>
        !order.IsDeleted && order.Status == DispatchStatus.Completed;

    public static decimal EarnedRevenue(IEnumerable<DispatchOrder> orders) =>
        orders.Where(IsEarnedTrip).Sum(x => x.TotalAmount);

    public static decimal EarnedPartnerPayable(IEnumerable<DispatchOrder> orders) =>
        orders.Where(IsEarnedTrip).Sum(x => x.PartnerPayableAmount);

    public static decimal EarnedGrossMargin(IEnumerable<DispatchOrder> orders) =>
        orders.Where(IsEarnedTrip).Sum(x => x.GrossMargin);

    public static int PendingExceptionCount(IEnumerable<DispatchOrder> orders) =>
        orders.Where(IsEarnedTrip)
            .SelectMany(x => x.TransportExceptions)
            .Count(x => x.Status is TransportExceptionStatus.Draft or TransportExceptionStatus.Submitted);
}
