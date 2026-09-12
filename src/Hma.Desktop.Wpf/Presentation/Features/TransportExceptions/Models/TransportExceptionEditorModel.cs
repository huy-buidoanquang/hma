using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Application.Features.TransportExceptions;

namespace Hma.Desktop.Wpf.Presentation.Features.TransportExceptions.Models;

public sealed partial class TransportExceptionEditorModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private int dispatchOrderId;
    [ObservableProperty] private ExceptionOrderOption? dispatchOrder;
    [ObservableProperty] private int transportExceptionCodeId;
    [ObservableProperty] private string codeSnapshot = "";
    [ObservableProperty] private DateTime occurredAt = DateTime.Now;
    [ObservableProperty] private string description = "";
    [ObservableProperty] private decimal customerCharge;
    [ObservableProperty] private decimal partnerCost;
    [ObservableProperty] private TransportExceptionStatus status = TransportExceptionStatus.Draft;
    [ObservableProperty] private int? submittedByUserId;
    [ObservableProperty] private string? reviewNote;
    [ObservableProperty] private string? voidReason;

    public string StatusLabel => Status switch
    {
        TransportExceptionStatus.Draft => "Nháp",
        TransportExceptionStatus.Submitted => "Chờ duyệt",
        TransportExceptionStatus.Approved => "Đã duyệt",
        TransportExceptionStatus.Rejected => "Từ chối",
        TransportExceptionStatus.Voided => "Đã hủy",
        _ => Status.ToString(),
    };

    public static TransportExceptionEditorModel From(TransportExceptionSummary item) => new()
    {
        Id = item.Id,
        DispatchOrderId = item.DispatchOrderId,
        DispatchOrder = item.DispatchOrder,
        TransportExceptionCodeId = item.TransportExceptionCodeId,
        CodeSnapshot = item.CodeSnapshot,
        OccurredAt = item.OccurredAt,
        Description = item.Description,
        CustomerCharge = item.CustomerCharge,
        PartnerCost = item.PartnerCost,
        Status = item.Status,
        SubmittedByUserId = item.SubmittedByUserId,
        ReviewNote = item.ReviewNote,
        VoidReason = item.VoidReason,
    };
}
