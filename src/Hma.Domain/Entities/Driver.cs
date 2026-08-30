namespace Hma.Domain.Entities;

public class Driver : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? IdentityNumber { get; set; }
    public int PartnerId { get; set; }
    public Partner? Partner { get; set; }
}
