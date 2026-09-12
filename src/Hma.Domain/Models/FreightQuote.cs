namespace Hma.Domain.Models;

public sealed class FreightQuote
{
    public int PriceListItemId { get; init; }
    public string PriceListCode { get; init; } = "";
    public decimal UnitPrice { get; init; }
    public decimal Surcharge { get; init; }
    public string SourceLabel { get; init; } = "";
}
