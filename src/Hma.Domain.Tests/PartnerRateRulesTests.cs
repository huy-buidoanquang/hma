using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class PartnerRateRulesTests
{
    [Fact]
    public void Save_requires_partner_route_vehicle_type_and_valid_money()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PartnerRateRules.EnsureCanSave(new PartnerRate()));

        PartnerRateRules.EnsureCanSave(new PartnerRate
        {
            PartnerId = 1,
            RouteId = 2,
            VehicleTypeId = 3,
            UnitPrice = 1_000_000,
            Surcharge = 100_000
        });
    }

    [Theory]
    [InlineData("2026-01-01", "2026-01-31", "2026-01-31", "2026-02-28", true)]
    [InlineData("2026-01-01", "2026-01-30", "2026-01-31", "2026-02-28", false)]
    [InlineData("2026-01-01", null, "2030-01-01", null, true)]
    public void Period_overlap_includes_boundary(
        string firstFrom,
        string? firstTo,
        string secondFrom,
        string? secondTo,
        bool expected)
    {
        var overlap = PartnerRateRules.PeriodsOverlap(
            DateTime.Parse(firstFrom), firstTo is null ? null : DateTime.Parse(firstTo),
            DateTime.Parse(secondFrom), secondTo is null ? null : DateTime.Parse(secondTo));

        Assert.Equal(expected, overlap);
    }
}
