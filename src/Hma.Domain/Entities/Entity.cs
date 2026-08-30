namespace Hma.Domain.Entities;

public abstract class Entity
{
    public int Id { get; set; }
    public int? LegacyId { get; set; }
}
