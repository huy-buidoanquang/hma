using Hma.Domain.Models;

namespace Hma.Desktop.Wpf.Presentation.Features.Dispatching.Models;

public sealed record FreightMatchState(
    bool IsMatched,
    bool IsMissing,
    string? PriceListCode,
    string SourceLabel)
{
    public static FreightMatchState FromLookup(bool hasRequiredInputs, FreightQuote? quote)
    {
        if (!hasRequiredInputs)
            return Empty;
        return quote is null
            ? new(false, true, null, "")
            : new(true, false, quote.PriceListCode, quote.SourceLabel);
    }

    public static FreightMatchState FromSaved(
        bool hasRequiredInputs,
        int? priceListItemId,
        string? priceListCode,
        string? sourceLabel)
    {
        if (!hasRequiredInputs)
            return Empty;
        var matched = priceListItemId is not null && !string.IsNullOrWhiteSpace(priceListCode);
        return matched
            ? new(true, false, priceListCode, sourceLabel ?? "")
            : new(false, true, null, "");
    }

    private static FreightMatchState Empty { get; } = new(false, false, null, "");
}
