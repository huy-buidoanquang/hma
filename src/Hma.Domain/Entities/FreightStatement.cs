namespace Hma.Domain.Entities;

public class FreightStatement : Entity
{
    public string Code { get; set; } = "";
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public FinancialDocumentStatus Status { get; set; } = FinancialDocumentStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public int? SubmittedByUserId { get; set; }
    public AppUser? SubmittedByUser { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public int? FinalizedByUserId { get; set; }
    public AppUser? FinalizedByUser { get; set; }
    public DateTime? VoidedAt { get; set; }
    public int? VoidedByUserId { get; set; }
    public AppUser? VoidedByUser { get; set; }
    public string? VoidReason { get; set; }
    public int TripCount { get; set; }
    public decimal FreightTotal { get; set; }
    public decimal SurchargeTotal { get; set; }
    public decimal ExtraCostTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalWithVat { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<FreightStatementLine> Lines { get; set; } = new List<FreightStatementLine>();
}
