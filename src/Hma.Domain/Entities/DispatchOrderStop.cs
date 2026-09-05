namespace Hma.Domain.Entities;

public class DispatchOrderStop : Entity
{
    public int DispatchOrderId { get; set; }
    public DispatchOrder? DispatchOrder { get; set; }
    public int Sequence { get; set; }
    public int LocationId { get; set; }
    public Location? Location { get; set; }
    public string NameSnapshot { get; set; } = "";
}
