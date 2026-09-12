using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class DispatchWorkflowRulesTests
{
    [Theory]
    [InlineData(ReconciliationStatus.Submitted)]
    [InlineData(ReconciliationStatus.Reconciled)]
    public void Edit_rejects_order_in_financial_workflow(ReconciliationStatus status)
    {
        var order = new DispatchOrder
        {
            Status = DispatchStatus.Completed,
            ReconciliationStatus = status
        };

        Assert.Throws<InvalidOperationException>(() => DispatchWorkflowRules.EnsureCanEdit(order));
        Assert.False(order.CanEdit);
    }

    [Fact]
    public void Edit_rejects_confirmed_order()
    {
        var order = new DispatchOrder
        {
            Status = DispatchStatus.Completed,
            ConfirmedAt = DateTime.Now
        };

        Assert.Throws<InvalidOperationException>(() => DispatchWorkflowRules.EnsureCanEdit(order));
        Assert.False(order.CanEdit);
    }

    [Fact]
    public void Delete_rejects_completed_order()
    {
        var order = new DispatchOrder { Status = DispatchStatus.Completed };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            DispatchWorkflowRules.EnsureCanDelete(order));

        Assert.Contains("nháp hoặc đã phát hành", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ReconciliationStatus.Submitted)]
    [InlineData(ReconciliationStatus.Reconciled)]
    public void MutateDocuments_rejects_order_in_reconciliation_workflow(ReconciliationStatus status)
    {
        var order = new DispatchOrder
        {
            Status = DispatchStatus.Completed,
            ReconciliationStatus = status
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            DispatchWorkflowRules.EnsureCanMutateDocuments(order));

        Assert.Contains("chứng từ", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MutateDocuments_accepts_confirmed_completed_order_before_submission()
    {
        var order = new DispatchOrder
        {
            Status = DispatchStatus.Completed,
            ConfirmedAt = DateTime.Now,
            ReconciliationStatus = ReconciliationStatus.Pending
        };

        DispatchWorkflowRules.EnsureCanMutateDocuments(order);
    }

    [Theory]
    [InlineData(DispatchStatus.Draft, DispatchStatus.Issued)]
    [InlineData(DispatchStatus.Issued, DispatchStatus.Completed)]
    [InlineData(DispatchStatus.Draft, DispatchStatus.Cancelled)]
    [InlineData(DispatchStatus.Issued, DispatchStatus.Cancelled)]
    public void ChangeStatus_allows_supported_transition(DispatchStatus current, DispatchStatus target)
    {
        var order = new DispatchOrder { Status = current };

        DispatchWorkflowRules.EnsureCanChangeStatus(order, target);
    }

    [Fact]
    public void ChangeStatus_rejects_skipping_directly_to_completed()
    {
        var order = new DispatchOrder { Status = DispatchStatus.Draft };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            DispatchWorkflowRules.EnsureCanChangeStatus(order, DispatchStatus.Completed));

        Assert.Contains("Không thể chuyển", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reopen_completed_rejects_confirmed_or_reconciled_order()
    {
        var confirmed = new DispatchOrder
        {
            Status = DispatchStatus.Completed,
            ConfirmedAt = DateTime.Now
        };
        var reconciled = new DispatchOrder
        {
            Status = DispatchStatus.Completed,
            ReconciliationStatus = ReconciliationStatus.Reconciled
        };

        Assert.Throws<InvalidOperationException>(() =>
            DispatchWorkflowRules.EnsureCanChangeStatus(confirmed, DispatchStatus.Issued));
        Assert.Throws<InvalidOperationException>(() =>
            DispatchWorkflowRules.EnsureCanChangeStatus(reconciled, DispatchStatus.Issued));
    }

    [Fact]
    public void SubmitReconciliation_requires_completed_order_and_delivery_note()
    {
        var issued = ValidOrder(DispatchStatus.Issued, ReconciliationStatus.Pending, hasDeliveryNote: true);
        var missingDocument = ValidOrder(DispatchStatus.Completed, ReconciliationStatus.Pending, hasDeliveryNote: false);

        Assert.Throws<InvalidOperationException>(() =>
            DispatchWorkflowRules.EnsureCanSubmitReconciliation(issued, 1));
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DispatchWorkflowRules.EnsureCanSubmitReconciliation(missingDocument, 1));
        Assert.Contains("biên bản giao hàng", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Confirm_requires_completed_order_and_unreconciled_state()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DispatchWorkflowRules.EnsureCanConfirm(new DispatchOrder { Status = DispatchStatus.Issued }));
        Assert.Throws<InvalidOperationException>(() => DispatchWorkflowRules.EnsureCanConfirm(new DispatchOrder
        {
            Status = DispatchStatus.Completed,
            ReconciliationStatus = ReconciliationStatus.Reconciled
        }));
        DispatchWorkflowRules.EnsureCanConfirm(new DispatchOrder { Status = DispatchStatus.Completed });
    }

    [Fact]
    public void Unconfirm_rejects_unconfirmed_or_submitted_order()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DispatchWorkflowRules.EnsureCanUnconfirm(new DispatchOrder { Status = DispatchStatus.Completed }));
        Assert.Throws<InvalidOperationException>(() => DispatchWorkflowRules.EnsureCanUnconfirm(new DispatchOrder
        {
            Status = DispatchStatus.Completed,
            ConfirmedAt = DateTime.Now,
            ReconciliationStatus = ReconciliationStatus.Submitted
        }));
        DispatchWorkflowRules.EnsureCanUnconfirm(new DispatchOrder
        {
            Status = DispatchStatus.Completed,
            ConfirmedAt = DateTime.Now
        });
    }

    [Theory]
    [InlineData(ReconciliationStatus.Pending)]
    [InlineData(ReconciliationStatus.Rejected)]
    public void SubmitReconciliation_accepts_pending_or_rejected(ReconciliationStatus status)
    {
        var order = ValidOrder(DispatchStatus.Completed, status, hasDeliveryNote: true);

        DispatchWorkflowRules.EnsureCanSubmitReconciliation(order, 10);
    }

    [Fact]
    public void ApproveReconciliation_enforces_maker_checker()
    {
        var order = ValidOrder(DispatchStatus.Completed, ReconciliationStatus.Submitted, hasDeliveryNote: true);
        order.ReconciliationSubmittedByUserId = 10;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            DispatchWorkflowRules.EnsureCanApproveReconciliation(order, 10));
        Assert.Contains("không được tự duyệt", ex.Message, StringComparison.OrdinalIgnoreCase);
        DispatchWorkflowRules.EnsureCanApproveReconciliation(order, 11);
    }

    [Fact]
    public void RejectReconciliation_requires_checker_and_reason()
    {
        var order = ValidOrder(DispatchStatus.Completed, ReconciliationStatus.Submitted, hasDeliveryNote: true);
        order.ReconciliationSubmittedByUserId = 10;

        Assert.Throws<InvalidOperationException>(() =>
            DispatchWorkflowRules.EnsureCanRejectReconciliation(order, 10, "Sai giá"));
        Assert.Throws<InvalidOperationException>(() =>
            DispatchWorkflowRules.EnsureCanRejectReconciliation(order, 11, " "));
        DispatchWorkflowRules.EnsureCanRejectReconciliation(order, 11, "Sai giá");
    }

    [Fact]
    public void StatementEligibility_requires_all_three_business_invariants()
    {
        DispatchWorkflowRules.EnsureCanIncludeInStatement(
            ValidOrder(DispatchStatus.Completed, ReconciliationStatus.Reconciled, hasDeliveryNote: true));

        Assert.Throws<InvalidOperationException>(() => DispatchWorkflowRules.EnsureCanIncludeInStatement(
            ValidOrder(DispatchStatus.Issued, ReconciliationStatus.Reconciled, hasDeliveryNote: true)));
        Assert.Throws<InvalidOperationException>(() => DispatchWorkflowRules.EnsureCanIncludeInStatement(
            ValidOrder(DispatchStatus.Completed, ReconciliationStatus.Pending, hasDeliveryNote: true)));
        Assert.Throws<InvalidOperationException>(() => DispatchWorkflowRules.EnsureCanIncludeInStatement(
            ValidOrder(DispatchStatus.Completed, ReconciliationStatus.Reconciled, hasDeliveryNote: false)));
    }

    private static DispatchOrder ValidOrder(
        DispatchStatus status,
        ReconciliationStatus reconciliationStatus,
        bool hasDeliveryNote)
    {
        var order = new DispatchOrder
        {
            Status = status,
            ReconciliationStatus = reconciliationStatus
        };
        if (hasDeliveryNote)
        {
            order.Documents.Add(new DispatchDocument
            {
                Kind = DispatchDocumentKind.DeliveryNote,
                FileName = "pod.pdf",
                StoredPath = "1/pod.pdf"
            });
        }

        return order;
    }
}
