namespace Hma.Domain.Entities;

public class UserPermission : Entity
{
    public int AppUserId { get; set; }
    public AppUser? AppUser { get; set; }
    public int AppScreenId { get; set; }
    public AppScreen? AppScreen { get; set; }
    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanView { get; set; }
    public bool CanPrint { get; set; }
}
