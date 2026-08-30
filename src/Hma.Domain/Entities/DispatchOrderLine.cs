namespace Hma.Domain.Entities;

public class DispatchOrderLine : Entity
{
    public int DispatchOrderId { get; set; }
    public DispatchOrder? DispatchOrder { get; set; }
    public int LineNumber { get; set; }
    public string? GoodsName { get; set; }
    public int? PackageCount { get; set; }
    public string? Route { get; set; }
    public decimal? Kilometers { get; set; }
    public string? Notes { get; set; }
}
