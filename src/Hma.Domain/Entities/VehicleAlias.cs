namespace Hma.Domain.Entities;

public class VehicleAlias
{
    public int Id { get; set; }
    public string Alias { get; set; } = "";
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
}
