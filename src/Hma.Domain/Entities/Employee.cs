namespace Hma.Domain.Entities;

public class Employee : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? IdentityNumber { get; set; }
    public string? VehiclePlate { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int? JobTitleId { get; set; }
    public JobTitle? JobTitle { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
