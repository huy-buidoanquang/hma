namespace Hma.Domain.Entities;

public class CustomerAlias
{
    public int Id { get; set; }
    public string Alias { get; set; } = "";
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
}
