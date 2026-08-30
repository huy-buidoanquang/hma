namespace Hma.Domain.Entities;

public class DispatchDocument : Entity
{
    public int DispatchOrderId { get; set; }
    public DispatchOrder? DispatchOrder { get; set; }
    public DispatchDocumentKind Kind { get; set; }
    public string FileName { get; set; } = "";
    public string StoredPath { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public int? UploadedByUserId { get; set; }
    public AppUser? UploadedByUser { get; set; }
}
