using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class DispatchAssignmentRules
{
    public static void EnsureSamePartner(Vehicle vehicle, Driver driver)
    {
        if (vehicle.PartnerId != driver.PartnerId)
            throw new InvalidOperationException("Xe và tài xế phải thuộc cùng một đối tác vận tải.");
    }
}
