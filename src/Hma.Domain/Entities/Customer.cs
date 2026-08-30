namespace Hma.Domain.Entities;

public class Customer : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public int? CityId { get; set; }
    public City? City { get; set; }
    public int? AccountantEmployeeId { get; set; }
    public Employee? AccountantEmployee { get; set; }
    public bool IsWalkIn { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
