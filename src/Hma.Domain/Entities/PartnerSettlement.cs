namespace Hma.Domain.Entities;

public class PartnerSettlement : Entity
{
    public string Code { get; set; } = "";
    public int PartnerId { get; set; }
    public Partner? Partner { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public FinancialDocumentStatus Status { get; set; } = FinancialDocumentStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
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
    public decimal GrossAmount { get; set; }
    public decimal OperatingFeeAmount { get; set; }
    public decimal PayableAmount { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<PartnerSettlementLine> Lines { get; set; } = new List<PartnerSettlementLine>();
}
