namespace Hma.Domain.Entities;

public class RouteAlias
{
    public int Id { get; set; }
    public string Alias { get; set; } = "";
    public int RouteId { get; set; }
    public Route? Route { get; set; }
}
