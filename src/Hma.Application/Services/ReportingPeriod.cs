namespace Hma.Application.Services;

public readonly record struct ReportingPeriod(DateTime StartInclusive, DateTime EndExclusive)
{
    public static ReportingPeriod InclusiveDays(DateTime from, DateTime to) =>
        new(from.Date, to.Date.AddDays(1));
}
