using Hma.Desktop.Wpf.Presentation.Features.Dispatching.Models;
using Hma.Domain.Models;

namespace Hma.Desktop.Wpf.Tests;

public class FreightMatchStateTests
{
    [Fact]
    public void Complete_inputs_and_quote_show_matching_price_list()
    {
        var quote = new FreightQuote
        {
            PriceListItemId = 12,
            PriceListCode = "BG-2026",
            UnitPrice = 1_000_000,
            SourceLabel = "Giá công bố"
        };

        var state = FreightMatchState.FromLookup(true, quote);

        Assert.True(state.IsMatched);
        Assert.False(state.IsMissing);
        Assert.Equal("BG-2026", state.PriceListCode);
        Assert.Equal("Giá công bố", state.SourceLabel);
    }

    [Fact]
    public void Complete_inputs_without_quote_show_warning()
    {
        var state = FreightMatchState.FromLookup(true, null);

        Assert.False(state.IsMatched);
        Assert.True(state.IsMissing);
        Assert.Null(state.PriceListCode);
        Assert.Empty(state.SourceLabel);
    }

    [Fact]
    public void Incomplete_inputs_show_neither_match_nor_warning()
    {
        var state = FreightMatchState.FromSaved(false, 12, "BG-2026", "Giá công bố");

        Assert.False(state.IsMatched);
        Assert.False(state.IsMissing);
        Assert.Null(state.PriceListCode);
    }

    [Fact]
    public void Saved_price_list_item_restores_match_badge()
    {
        var state = FreightMatchState.FromSaved(true, 12, "BG-2026", "Giá công bố");

        Assert.True(state.IsMatched);
        Assert.False(state.IsMissing);
        Assert.Equal("BG-2026", state.PriceListCode);
    }
}
