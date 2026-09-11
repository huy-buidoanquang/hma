using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class TransportExceptionRules
{
    public static void EnsureCanSave(
        TransportException item,
        DispatchOrder order,
        TransportExceptionCode code)
    {
        EnsureOrderOpen(order);
        if (!code.IsActive)
            throw new InvalidOperationException("Mã sự cố đã ngừng sử dụng.");
        if (item.Status is not TransportExceptionStatus.Draft and not TransportExceptionStatus.Rejected)
            throw new InvalidOperationException("Chỉ được sửa sự cố nháp hoặc đã bị từ chối.");
        if (string.IsNullOrWhiteSpace(item.Description))
            throw new InvalidOperationException("Cần mô tả sự cố vận tải.");
        MoneyRules.EnsureNonNegative(item.CustomerCharge, "Khoản thu khách hàng");
        MoneyRules.EnsureNonNegative(item.PartnerCost, "Chi phí đối tác");
        if (item.PartnerCost > 0 && order.PartnerId is null)
            throw new InvalidOperationException("Không thể ghi chi phí đối tác khi lệnh chưa gán nhà xe.");
    }

    public static void EnsureCanSubmit(TransportException item, int? userId)
    {
        if (userId is null)
            throw new InvalidOperationException("Không xác định được người gửi duyệt sự cố.");
        if (item.Status is not TransportExceptionStatus.Draft and not TransportExceptionStatus.Rejected)
            throw new InvalidOperationException("Chỉ được gửi duyệt sự cố nháp hoặc đã bị từ chối.");
    }

    public static void EnsureCanDelete(TransportException item)
    {
        if (item.Status is not TransportExceptionStatus.Draft and not TransportExceptionStatus.Rejected)
        {
            throw new InvalidOperationException(
                "Chỉ được xóa sự cố nháp hoặc đã bị từ chối; sự cố đã gửi duyệt phải được giữ làm lịch sử.");
        }
    }

    public static void EnsureCanReview(
        TransportException item,
        DispatchOrder order,
        int? userId,
        bool isManager,
        string? rejectionReason = null)
    {
        EnsureOrderOpen(order);
        if (!isManager)
            throw new InvalidOperationException("Chỉ quản lý được duyệt hoặc từ chối chi phí sự cố.");
        if (userId is null)
            throw new InvalidOperationException("Không xác định được người duyệt sự cố.");
        if (item.Status != TransportExceptionStatus.Submitted)
            throw new InvalidOperationException("Sự cố chưa ở trạng thái chờ duyệt.");
        if (item.SubmittedByUserId == userId)
            throw new InvalidOperationException("Người gửi duyệt không được tự duyệt sự cố.");
        if (rejectionReason is not null && string.IsNullOrWhiteSpace(rejectionReason))
            throw new InvalidOperationException("Cần nhập lý do từ chối sự cố.");
    }

    public static void EnsureCanVoid(
        TransportException item,
        DispatchOrder order,
        bool isManager,
        string? reason)
    {
        EnsureOrderOpen(order);
        if (!isManager)
            throw new InvalidOperationException("Chỉ quản lý được hủy sự cố đã duyệt.");
        if (item.Status != TransportExceptionStatus.Approved)
            throw new InvalidOperationException("Chỉ được hủy sự cố đã duyệt.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Cần nhập lý do hủy sự cố.");
    }

    public static void EnsureNoOpenExceptions(DispatchOrder order)
    {
        if (order.TransportExceptions.Any(x =>
                x.Status is TransportExceptionStatus.Draft or TransportExceptionStatus.Submitted))
        {
            throw new InvalidOperationException(
                "Cần xử lý hết sự cố nháp hoặc đang chờ duyệt trước khi đối soát.");
        }
    }

    public static void EnsurePartnerAssignmentCanChange(DispatchOrder order, int? newPartnerId)
    {
        if (order.ApprovedExceptionCost > 0 && order.PartnerId != newPartnerId)
            throw new InvalidOperationException(
                "Không thể đổi nhà xe khi lệnh đã có chi phí sự cố được duyệt cho đối tác.");
    }

    private static void EnsureOrderOpen(DispatchOrder order)
    {
        if (order.IsDeleted || order.Status == DispatchStatus.Cancelled)
            throw new InvalidOperationException("Không thể thay đổi sự cố của lệnh đã xóa hoặc đã hủy.");
        if (order.ReconciliationStatus is ReconciliationStatus.Submitted or ReconciliationStatus.Reconciled)
            throw new InvalidOperationException("Không thể thay đổi sự cố sau khi lệnh đã gửi đối soát.");
    }
}
