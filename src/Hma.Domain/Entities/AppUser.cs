namespace Hma.Domain.Entities;

public class AppUser : Entity
{
    public string UserName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? DisplayName { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public bool IsManager { get; set; }
    public bool IsSpecial { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public ICollection<UserPermission> Permissions { get; set; } = new List<UserPermission>();
}
