namespace Hma.Application.Features.TransportExceptions;

public sealed record TransportExceptionSummary(
    int Id,
    int DispatchOrderId,
    ExceptionOrderOption? DispatchOrder,
    int TransportExceptionCodeId,
    TransportExceptionCodeOption? ExceptionCode,
    string CodeSnapshot,
    string NameSnapshot,
    DateTime OccurredAt,
    string Description,
    decimal CustomerCharge,
    decimal PartnerCost,
    TransportExceptionStatus Status,
    int? SubmittedByUserId,
    string? ReviewNote,
    string? VoidReason,
    byte[] VersionToken)
{
    public string StatusLabel => Status switch
    {
        TransportExceptionStatus.Draft => "Nháp",
        TransportExceptionStatus.Submitted => "Chờ duyệt",
        TransportExceptionStatus.Approved => "Đã duyệt",
        TransportExceptionStatus.Rejected => "Từ chối",
        TransportExceptionStatus.Voided => "Đã hủy",
        _ => Status.ToString(),
    };
}
