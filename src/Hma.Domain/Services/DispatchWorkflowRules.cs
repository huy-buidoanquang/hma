using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class DispatchWorkflowRules
{
    public static void EnsureCanEdit(DispatchOrder order)
    {
        if (order.IsDeleted)
            throw new InvalidOperationException("Lệnh đã xóa nên không thể chỉnh sửa.");
        if (order.Status is DispatchStatus.Locked or DispatchStatus.Cancelled)
            throw new InvalidOperationException("Lệnh đã khóa hoặc đã hủy nên không thể chỉnh sửa.");
        if (order.ConfirmedAt is not null)
            throw new InvalidOperationException("Lệnh đã chốt nên không thể chỉnh sửa.");
        if (order.ReconciliationStatus is ReconciliationStatus.Submitted or ReconciliationStatus.Reconciled)
            throw new InvalidOperationException("Lệnh đã gửi hoặc đã duyệt đối soát nên không thể chỉnh sửa.");
    }

    public static void EnsureCanDelete(DispatchOrder order)
    {
        EnsureCanEdit(order);
        if (order.Status is not DispatchStatus.Draft and not DispatchStatus.Issued)
            throw new InvalidOperationException("Chỉ được xóa lệnh nháp hoặc đã phát hành chưa hoàn thành.");
    }

    public static void EnsureCanMutateDocuments(DispatchOrder order)
    {
        if (order.IsDeleted)
            throw new InvalidOperationException("Lệnh đã xóa nên không thể thay đổi chứng từ.");
        if (order.Status == DispatchStatus.Cancelled)
            throw new InvalidOperationException("Lệnh đã hủy nên không thể thay đổi chứng từ.");
        if (order.ReconciliationStatus is ReconciliationStatus.Submitted or ReconciliationStatus.Reconciled)
            throw new InvalidOperationException("Lệnh đã gửi hoặc đã duyệt đối soát nên không thể thay đổi chứng từ.");
    }

    public static void EnsureCanChangeStatus(DispatchOrder order, DispatchStatus target)
    {
        if (order.Status == target)
            return;

        var allowed = (order.Status, target) switch
        {
            (DispatchStatus.Draft, DispatchStatus.Issued) => true,
            (DispatchStatus.Issued, DispatchStatus.Completed) => true,
            (DispatchStatus.Completed, DispatchStatus.Issued) =>
                order.ConfirmedAt is null
                && order.ReconciliationStatus is ReconciliationStatus.Pending or ReconciliationStatus.Rejected,
            (DispatchStatus.Draft, DispatchStatus.Cancelled) => true,
            (DispatchStatus.Issued, DispatchStatus.Cancelled) => true,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException(
                $"Không thể chuyển trạng thái lệnh từ {order.Status} sang {target}.");
    }

    public static void EnsureCanConfirm(DispatchOrder order)
    {
        if (order.Status != DispatchStatus.Completed)
            throw new InvalidOperationException("Chỉ được chốt lệnh đã hoàn thành chuyến.");
        if (order.ReconciliationStatus == ReconciliationStatus.Reconciled)
            throw new InvalidOperationException("Lệnh đã đối soát nên không cần chốt lại.");
    }

    public static void EnsureCanUnconfirm(DispatchOrder order)
    {
        if (order.ConfirmedAt is null && order.Status != DispatchStatus.Locked)
            throw new InvalidOperationException("Lệnh chưa được chốt.");
        if (order.ReconciliationStatus is ReconciliationStatus.Submitted or ReconciliationStatus.Reconciled)
            throw new InvalidOperationException("Không thể bỏ chốt lệnh đã gửi hoặc đã duyệt đối soát.");
    }

    public static void EnsureCanSubmitReconciliation(DispatchOrder order, int? submittedByUserId)
    {
        TransportExceptionRules.EnsureNoOpenExceptions(order);
        if (submittedByUserId is null)
            throw new InvalidOperationException("Không xác định được người gửi đối soát.");
        if (order.Status != DispatchStatus.Completed)
            throw new InvalidOperationException("Chỉ gửi đối soát lệnh đã hoàn thành chuyến.");
        if (!order.HasDeliveryNote)
            throw new InvalidOperationException("Cần có biên bản giao hàng trước khi gửi đối soát.");
        if (order.ReconciliationStatus is not ReconciliationStatus.Pending and not ReconciliationStatus.Rejected)
            throw new InvalidOperationException("Lệnh không ở trạng thái có thể gửi đối soát.");
    }

    public static void EnsureCanApproveReconciliation(DispatchOrder order, int? approvedByUserId)
    {
        if (approvedByUserId is null)
            throw new InvalidOperationException("Không xác định được người duyệt đối soát.");
        if (order.Status != DispatchStatus.Completed)
            throw new InvalidOperationException("Chỉ đối soát lệnh đã hoàn thành chuyến.");
        if (!order.HasDeliveryNote)
            throw new InvalidOperationException("Cần có biên bản giao hàng trước khi đối soát.");
        if (order.ReconciliationStatus != ReconciliationStatus.Submitted)
            throw new InvalidOperationException("Lệnh chưa được gửi đối soát.");
        if (order.ReconciliationSubmittedByUserId == approvedByUserId)
            throw new InvalidOperationException("Người gửi đối soát không được tự duyệt.");
    }

    public static void EnsureCanRejectReconciliation(
        DispatchOrder order,
        int? rejectedByUserId,
        string? reason)
    {
        if (rejectedByUserId is null)
            throw new InvalidOperationException("Không xác định được người từ chối đối soát.");
        if (order.ReconciliationStatus != ReconciliationStatus.Submitted)
            throw new InvalidOperationException("Chỉ từ chối lệnh đang chờ duyệt đối soát.");
        if (order.ReconciliationSubmittedByUserId == rejectedByUserId)
            throw new InvalidOperationException("Người gửi đối soát không được tự từ chối.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Cần nhập lý do từ chối đối soát.");
    }

    public static void EnsureCanIncludeInStatement(DispatchOrder order)
    {
        if (order.Status != DispatchStatus.Completed
            || order.ReconciliationStatus != ReconciliationStatus.Reconciled
            || !order.HasDeliveryNote)
        {
            throw new InvalidOperationException(
                "Bảng kê chỉ nhận lệnh đã hoàn thành, đã đối soát và có biên bản giao hàng.");
        }
    }
}
