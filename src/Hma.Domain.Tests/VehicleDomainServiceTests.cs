using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class VehicleDomainServiceTests
{
    [Fact]
    public void Requires_plate_and_partner()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            VehicleDomainService.EnsureCanSave(new Vehicle { PlateNumber = " ", PartnerId = 0 }));
        Assert.Contains("Biển số", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_negative_tonnage()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            VehicleDomainService.EnsureCanSave(new Vehicle { PlateNumber = "51C-12345", PartnerId = 1, Tonnage = -1 }));
        Assert.Contains("Tải trọng", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Accepts_valid_vehicle()
    {
        VehicleDomainService.EnsureCanSave(new Vehicle { PlateNumber = "51C-12345", PartnerId = 1, Tonnage = 8 });
    }

    [Fact]
    public void Canonicalizes_compact_and_lowercase_plates()
    {
        var vehicle = new Vehicle { PlateNumber = "29c23456", PartnerId = 1 };
        VehicleDomainService.EnsureCanSave(vehicle);
        Assert.Equal("29C-23456", vehicle.PlateNumber);
    }

    [Fact]
    public void Rejects_plate_not_matching_xxY_serial()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            VehicleDomainService.EnsureCanSave(new Vehicle { PlateNumber = "51C-1", PartnerId = 1 }));
        Assert.Contains("29C-23456", ex.Message, StringComparison.Ordinal);
    }
}
