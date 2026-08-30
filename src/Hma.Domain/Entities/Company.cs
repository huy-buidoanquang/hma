namespace Hma.Domain.Entities;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? TaxCode { get; set; }
    public string? Bank { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
}
