namespace Hma.Domain.Entities;

public class LocationAlias
{
    public int Id { get; set; }
    public string Alias { get; set; } = "";
    public int LocationId { get; set; }
    public Location? Location { get; set; }
    public LocationAliasKind Kind { get; set; } = LocationAliasKind.Both;
}
