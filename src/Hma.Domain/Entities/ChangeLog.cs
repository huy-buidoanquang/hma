namespace Hma.Domain.Entities;

public class ChangeLog
{
    public int Id { get; set; }
    public string EntityName { get; set; } = "";
    public int EntityId { get; set; }
    public string Action { get; set; } = "";
    public string? Summary { get; set; }
    public string? OldJson { get; set; }
    public string? NewJson { get; set; }
    public int? UserId { get; set; }
    public AppUser? User { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.Now;
}
