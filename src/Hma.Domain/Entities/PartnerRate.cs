namespace Hma.Domain.Entities;

public class PartnerRate : Entity
{
    public int PartnerId { get; set; }
    public Partner? Partner { get; set; }
    public int RouteId { get; set; }
    public Route? Route { get; set; }
    public int VehicleTypeId { get; set; }
    public VehicleType? VehicleType { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;
    public DateTime? EffectiveTo { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Surcharge { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
