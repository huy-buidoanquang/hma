using Hma.Domain.Entities;

namespace Hma.Domain.Rules;

public static class PartnerSettlementRules
{
    public static void EnsureOrderEligible(DispatchOrder order, int partnerId)
    {
        TransportExceptionRules.EnsureNoOpenExceptions(order);
        if (order.Status != DispatchStatus.Completed
            || !order.HasDeliveryNote
            || order.PartnerId != partnerId)
        {
            throw new InvalidOperationException(
                "Quyết toán đối tác chỉ nhận chuyến đã hoàn thành, có biên bản giao hàng và đúng đối tác.");
        }
        MoneyRules.EnsureNonNegative(order.PartnerPayableAmount, "Số phải trả đối tác");
    }

    public static void EnsureCanGenerate(PartnerSettlement? existing)
    {
        if (existing is not null && existing.Status != FinancialDocumentStatus.Draft)
            throw new InvalidOperationException("Chỉ được lập lại quyết toán đang ở trạng thái nháp.");
    }

    public static void EnsureCanSubmit(PartnerSettlement settlement, int? userId)
    {
        if (userId is null)
            throw new InvalidOperationException("Không xác định được người gửi duyệt.");
        if (settlement.Status != FinancialDocumentStatus.Draft)
            throw new InvalidOperationException("Chỉ quyết toán nháp mới được gửi duyệt.");
        if (settlement.Lines.Count == 0)
            throw new InvalidOperationException("Quyết toán không có chuyến để gửi duyệt.");
    }

    public static void EnsureCanFinalize(PartnerSettlement settlement, int? userId)
    {
        if (userId is null)
            throw new InvalidOperationException("Không xác định được người chốt.");
        if (settlement.Status != FinancialDocumentStatus.Submitted)
            throw new InvalidOperationException("Quyết toán chưa được gửi duyệt.");
        if (settlement.SubmittedByUserId == userId)
            throw new InvalidOperationException("Người gửi duyệt không được tự chốt quyết toán.");
    }

    public static void EnsureCanVoid(PartnerSettlement settlement, bool isManager, string? reason)
    {
        if (!isManager)
            throw new InvalidOperationException("Chỉ quản lý được hủy quyết toán đối tác.");
        if (settlement.Status == FinancialDocumentStatus.Voided)
            throw new InvalidOperationException("Quyết toán đã hủy.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Cần nhập lý do hủy quyết toán.");
    }
}
