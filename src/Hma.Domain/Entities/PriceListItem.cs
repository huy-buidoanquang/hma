namespace Hma.Domain.Entities;

public class PriceListItem : Entity
{
    public int PriceListRevisionId { get; set; }
    public PriceListRevision? PriceListRevision { get; set; }
    public int? PickupCityId { get; set; }
    public City? PickupCity { get; set; }
    public int DeliveryCityId { get; set; }
    public City? DeliveryCity { get; set; }
    public int VehicleTypeId { get; set; }
    public VehicleType? VehicleType { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Surcharge { get; set; }
}
