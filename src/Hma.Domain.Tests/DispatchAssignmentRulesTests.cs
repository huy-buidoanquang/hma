using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class DispatchAssignmentRulesTests
{
    [Fact]
    public void Assignment_rejects_vehicle_and_driver_from_different_partners()
    {
        var vehicle = new Vehicle { PartnerId = 1 };
        var driver = new Driver { PartnerId = 2 };

        Assert.Throws<InvalidOperationException>(() =>
            DispatchAssignmentRules.EnsureSamePartner(vehicle, driver));
    }

    [Fact]
    public void Assignment_accepts_same_partner()
    {
        DispatchAssignmentRules.EnsureSamePartner(
            new Vehicle { PartnerId = 1 },
            new Driver { PartnerId = 1 });
    }
}
