using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class DispatchKpiRulesTests
{
    [Fact]
    public void Earned_revenue_only_counts_completed_non_deleted_trips()
    {
        var orders = new[]
        {
            new DispatchOrder { Status = DispatchStatus.Draft, TotalAmount = 100 },
            new DispatchOrder { Status = DispatchStatus.Issued, TotalAmount = 200 },
            new DispatchOrder { Status = DispatchStatus.Completed, TotalAmount = 300 },
            new DispatchOrder { Status = DispatchStatus.Cancelled, TotalAmount = 400 },
            new DispatchOrder { Status = DispatchStatus.Completed, TotalAmount = 500, IsDeleted = true }
        };

        Assert.Equal(300, DispatchKpiRules.EarnedRevenue(orders));
    }

    [Fact]
    public void Margin_and_pending_exception_kpis_only_count_earned_trips()
    {
        var completed = new DispatchOrder
        {
            Status = DispatchStatus.Completed,
            PartnerPayableAmount = 700,
            GrossMargin = 300
        };
        completed.TransportExceptions.Add(new TransportException { Status = TransportExceptionStatus.Draft });
        completed.TransportExceptions.Add(new TransportException { Status = TransportExceptionStatus.Approved });
        var issued = new DispatchOrder
        {
            Status = DispatchStatus.Issued,
            PartnerPayableAmount = 900,
            GrossMargin = 100
        };
        issued.TransportExceptions.Add(new TransportException { Status = TransportExceptionStatus.Submitted });

        var orders = new[] { completed, issued };

        Assert.Equal(700, DispatchKpiRules.EarnedPartnerPayable(orders));
        Assert.Equal(300, DispatchKpiRules.EarnedGrossMargin(orders));
        Assert.Equal(1, DispatchKpiRules.PendingExceptionCount(orders));
    }
}
