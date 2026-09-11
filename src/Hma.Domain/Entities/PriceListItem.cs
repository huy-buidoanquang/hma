namespace Hma.Domain.Entities;

public class PriceListItem : Entity
{
    public int PriceListRevisionId { get; set; }
    public PriceListRevision? PriceListRevision { get; set; }
    public int? RouteId { get; set; }
    public Route? Route { get; set; }
    public int? DeliveryLocationId { get; set; }
    public Location? DeliveryLocation { get; set; }
    public int VehicleTypeId { get; set; }
    public VehicleType? VehicleType { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Surcharge { get; set; }
}
