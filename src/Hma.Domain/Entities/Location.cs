namespace Hma.Domain.Entities;

public class Location : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int? CityId { get; set; }
    public City? City { get; set; }
}
