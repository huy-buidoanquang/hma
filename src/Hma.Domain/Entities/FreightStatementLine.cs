namespace Hma.Domain.Entities;

public class FreightStatementLine : Entity
{
    public int FreightStatementId { get; set; }
    public FreightStatement? FreightStatement { get; set; }
    public int DispatchOrderId { get; set; }
    public DispatchOrder? DispatchOrder { get; set; }
    public DateTime TripDate { get; set; }
    public string DispatchCode { get; set; } = "";
    public string? Route { get; set; }
    public string? PlateNumber { get; set; }
    public string? Tonnage { get; set; }
    public string? DriverName { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Surcharge { get; set; }
    public decimal ExtraCost { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
}
