namespace Hma.Domain.Entities;

public class PriceListRevision : Entity
{
    public int PriceListId { get; set; }
    public PriceList? PriceList { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime? CreatedAt { get; set; }
    public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();
}
