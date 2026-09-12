using Hma.Domain.Entities;

namespace Hma.Domain.Rules;

public static class FreightStatementRules
{
    public static void EnsureCanGenerate(FreightStatement? existing)
    {
        if (existing is not null && existing.Status != FinancialDocumentStatus.Draft)
            throw new InvalidOperationException("Chỉ được tổng hợp lại bảng kê đang ở trạng thái nháp.");
    }

    public static void EnsureCanSubmit(FreightStatement statement, int? submittedByUserId)
    {
        if (submittedByUserId is null)
            throw new InvalidOperationException("Không xác định được người gửi duyệt bảng kê.");
        if (statement.Status != FinancialDocumentStatus.Draft)
            throw new InvalidOperationException("Chỉ được gửi duyệt bảng kê nháp.");
        if (statement.Lines.Count == 0)
            throw new InvalidOperationException("Bảng kê không có chuyến nên không thể gửi duyệt.");
    }

    public static void EnsureCanFinalize(FreightStatement statement, int? finalizedByUserId)
    {
        if (finalizedByUserId is null)
            throw new InvalidOperationException("Không xác định được người duyệt bảng kê.");
        if (statement.Status != FinancialDocumentStatus.Submitted)
            throw new InvalidOperationException("Bảng kê chưa được gửi duyệt.");
        if (statement.SubmittedByUserId == finalizedByUserId)
            throw new InvalidOperationException("Người gửi bảng kê không được tự duyệt.");
    }

    public static void EnsureCanVoid(FreightStatement statement, bool isManager, string? reason)
    {
        if (!isManager)
            throw new InvalidOperationException("Chỉ quản lý được hủy bảng kê đã chốt.");
        if (statement.Status != FinancialDocumentStatus.Finalized)
            throw new InvalidOperationException("Chỉ được hủy bảng kê đã chốt.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Cần nhập lý do hủy bảng kê.");
    }
}
