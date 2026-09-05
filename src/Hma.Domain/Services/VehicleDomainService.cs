using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class VehicleDomainService
{
    public static void EnsureCanSave(Vehicle vehicle)
    {
        if (string.IsNullOrWhiteSpace(vehicle.PlateNumber))
            throw new InvalidOperationException("Biển số xe là bắt buộc.");
        vehicle.PlateNumber = VehiclePlateRules.Canonicalize(vehicle.PlateNumber);
        if (vehicle.PartnerId == 0)
            throw new InvalidOperationException("Xe phải thuộc một đối tác.");
        if (vehicle.Tonnage is < 0)
            throw new InvalidOperationException("Tải trọng không được âm.");
    }
}
