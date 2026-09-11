using Hma.Desktop.Wpf.ViewModels;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.Tests;

public class ReconcileWorkspaceViewModelTests
{
    [Fact]
    public void Checklist_includes_manual_and_approved_exception_revenue()
    {
        var order = new DispatchOrder
        {
            UnitPrice = 1_000_000,
            Surcharge = 10_000,
            ExtraCost = 20_000,
            ApprovedExceptionRevenue = 30_000,
            Status = DispatchStatus.Completed,
            ReconciliationStatus = ReconciliationStatus.Pending
        };
        order.RecalculateTotal();

        var checklist = ReconcileWorkspaceViewModel.BuildChecklist(order);

        Assert.Contains($"Phát sinh: {50_000:N0}", checklist, StringComparison.Ordinal);
        Assert.Contains($"Tổng: {1_060_000:N0}", checklist, StringComparison.Ordinal);
    }
}
