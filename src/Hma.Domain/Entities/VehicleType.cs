namespace Hma.Domain.Entities;

public class VehicleType : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Tonnage { get; set; }
}
