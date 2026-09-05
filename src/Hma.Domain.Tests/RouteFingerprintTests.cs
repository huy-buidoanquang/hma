using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class RouteFingerprintTests
{
    [Fact]
    public void Fingerprint_joins_ordered_ids()
    {
        Assert.Equal("10-20-30", RouteFingerprint.From([10, 20, 30]));
        Assert.Equal("Nội Bài → Hải Phòng", RouteFingerprint.FormatLabel(["Nội Bài", "Hải Phòng"]));
    }

    [Fact]
    public void Fingerprint_requires_two_points()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => RouteFingerprint.From([1]));
        Assert.Contains("hai điểm", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RouteRules_sets_fingerprint_from_stops()
    {
        var route = new Route
        {
            Code = "HN-HP",
            Name = "Hà Nội → Hải Phòng",
            Stops =
            [
                new RouteStop { Sequence = 0, LocationId = 1 },
                new RouteStop { Sequence = 1, LocationId = 2 }
            ]
        };
        RouteRules.EnsureCanSave(route);
        Assert.Equal("1-2", route.Fingerprint);
    }
}
