using Hma.Application.Services;

namespace Hma.Application.Tests;

public class ReportingPeriodTests
{
    [Fact]
    public void InclusiveDays_includes_the_whole_end_date()
    {
        var period = ReportingPeriod.InclusiveDays(
            new DateTime(2026, 8, 11, 15, 30, 0),
            new DateTime(2026, 9, 11, 8, 45, 0));

        Assert.Equal(new DateTime(2026, 8, 11), period.StartInclusive);
        Assert.Equal(new DateTime(2026, 9, 12), period.EndExclusive);
    }
}
