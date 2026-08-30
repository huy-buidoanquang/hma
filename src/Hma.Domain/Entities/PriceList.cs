namespace Hma.Domain.Entities;

public class PriceList : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? LockReason { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<PriceListRevision> Revisions { get; set; } = new List<PriceListRevision>();
}
