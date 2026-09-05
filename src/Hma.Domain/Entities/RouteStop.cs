namespace Hma.Domain.Entities;

public class RouteStop : Entity
{
    public int RouteId { get; set; }
    public Route? Route { get; set; }
    public int Sequence { get; set; }
    public int LocationId { get; set; }
    public Location? Location { get; set; }
}
