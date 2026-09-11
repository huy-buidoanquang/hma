namespace Hma.Domain.Entities;

public class PartnerSettlementLine : Entity
{
    public int PartnerSettlementId { get; set; }
    public PartnerSettlement? PartnerSettlement { get; set; }
    public int DispatchOrderId { get; set; }
    public DispatchOrder? DispatchOrder { get; set; }
    public DateTime TripDate { get; set; }
    public string DispatchCode { get; set; } = "";
    public string? Route { get; set; }
    public string? PlateNumber { get; set; }
    public string? DriverName { get; set; }
    public decimal BuyTotal { get; set; }
    public decimal OperatingFeePercent { get; set; }
    public decimal OperatingFeeAmount { get; set; }
    public decimal PayableAmount { get; set; }
}
