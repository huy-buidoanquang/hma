namespace Hma.Domain.Entities;

public class TransportExceptionCode : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string DisplayName => $"{Code} — {Name}";
}
