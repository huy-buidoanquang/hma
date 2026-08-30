namespace Hma.Domain.Entities;

public class City : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}
