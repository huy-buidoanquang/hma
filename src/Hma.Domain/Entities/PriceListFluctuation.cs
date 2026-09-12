using Hma.Domain.Enums;

namespace Hma.Domain.Entities;

public class PriceListFluctuation : Entity
{
    public int PriceListId { get; set; }
    public PriceList? PriceList { get; set; }
    public PriceFluctuationType Type { get; set; }
    public decimal Value { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;
    public DateTime? EffectiveTo { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
