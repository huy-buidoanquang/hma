using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class DriverRules
{
    public static void EnsureCanSave(Driver driver)
    {
        if (string.IsNullOrWhiteSpace(driver.Code) || string.IsNullOrWhiteSpace(driver.Name))
            throw new InvalidOperationException("Mã và tên tài xế là bắt buộc.");
        if (driver.PartnerId == 0)
            throw new InvalidOperationException("Tài xế phải thuộc một đối tác.");
        PhoneRules.EnsureOptional(driver.Phone);
    }
}
