using Hma.Desktop.Wpf.ViewModels;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.Tests;

public class DispatchGridRowTests
{
    [Theory]
    [InlineData(ReconciliationStatus.Submitted)]
    [InlineData(ReconciliationStatus.Reconciled)]
    public void FromOrder_disables_financially_locked_orders(ReconciliationStatus status)
    {
        var row = DispatchGridRow.FromOrder(new DispatchOrder
        {
            Status = DispatchStatus.Completed,
            ReconciliationStatus = status
        });

        Assert.False(row.CanEditRow);
    }

    [Fact]
    public void FromOrder_keeps_pending_order_editable_and_defaults_billing_period()
    {
        var pickupAt = new DateTime(2026, 9, 11, 20, 30, 0);

        var row = DispatchGridRow.FromOrder(new DispatchOrder
        {
            Status = DispatchStatus.Issued,
            ReconciliationStatus = ReconciliationStatus.Pending,
            PickupAt = pickupAt
        });

        Assert.True(row.CanEditRow);
        Assert.Equal(2026, row.BillingYear);
        Assert.Equal(9, row.BillingMonth);
    }
}
