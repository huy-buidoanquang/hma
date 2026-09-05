namespace Hma.Domain.Entities;

public class Partner : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal OperatingFeePercent { get; set; }
    public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
