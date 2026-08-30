using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class EmployeeRules
{
    public static void EnsureCanSave(Employee employee)
    {
        if (string.IsNullOrWhiteSpace(employee.Code) || string.IsNullOrWhiteSpace(employee.Name))
            throw new InvalidOperationException("Mã và tên nhân viên là bắt buộc.");
        PhoneRules.EnsureOptional(employee.Phone);
        PhoneRules.EnsureOptional(employee.Mobile, "Số di động");
    }
}
