namespace Hma.Domain.Entities;

public class TransportException : Entity
{
    public int DispatchOrderId { get; set; }
    public DispatchOrder? DispatchOrder { get; set; }
    public int TransportExceptionCodeId { get; set; }
    public TransportExceptionCode? ExceptionCode { get; set; }
    public string CodeSnapshot { get; set; } = "";
    public string NameSnapshot { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.Now;
    public string Description { get; set; } = "";
    public decimal CustomerCharge { get; set; }
    public decimal PartnerCost { get; set; }
    public TransportExceptionStatus Status { get; set; } = TransportExceptionStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? SubmittedByUserId { get; set; }
    public AppUser? SubmittedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedByUserId { get; set; }
    public AppUser? ReviewedByUser { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? VoidedAt { get; set; }
    public int? VoidedByUserId { get; set; }
    public AppUser? VoidedByUser { get; set; }
    public string? VoidReason { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public string StatusLabel => Status switch
    {
        TransportExceptionStatus.Draft => "Nháp",
        TransportExceptionStatus.Submitted => "Chờ duyệt",
        TransportExceptionStatus.Approved => "Đã duyệt",
        TransportExceptionStatus.Rejected => "Từ chối",
        TransportExceptionStatus.Voided => "Đã hủy",
        _ => Status.ToString()
    };
}
