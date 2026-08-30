namespace Hma.Domain.Entities;

public class Vehicle : Entity
{
    public string PlateNumber { get; set; } = "";
    public int PartnerId { get; set; }
    public Partner? Partner { get; set; }
    public int? VehicleTypeId { get; set; }
    public VehicleType? VehicleType { get; set; }
    public decimal? Tonnage { get; set; }
}
