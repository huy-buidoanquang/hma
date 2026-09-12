using Hma.Desktop.Wpf.Presentation.Features.Dispatching.Models;

namespace Hma.Desktop.Wpf.Tests;

public class PickupTimeSelectionTests
{
    [Fact]
    public void Round_trip_preserves_date_hour_and_minute()
    {
        var expected = new DateTime(2026, 9, 12, 13, 47, 36);

        var selection = PickupTimeSelection.From(expected);

        Assert.Equal(expected.Date, selection.Date);
        Assert.Equal(13, selection.Hour);
        Assert.Equal(47, selection.Minute);
        Assert.Equal(new DateTime(2026, 9, 12, 13, 47, 0), selection.ToDateTime());
    }
}
