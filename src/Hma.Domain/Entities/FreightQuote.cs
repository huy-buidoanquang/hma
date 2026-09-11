namespace Hma.Domain.Entities;

public sealed class FreightQuote
{
    public int PriceListItemId { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Surcharge { get; init; }
    public string SourceLabel { get; init; } = "";
}
