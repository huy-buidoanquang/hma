namespace Hma.Domain.Entities;

public class Route : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Fingerprint { get; set; } = "";
    public string? Description { get; set; }
    public ICollection<RouteStop> Stops { get; set; } = new List<RouteStop>();
}
