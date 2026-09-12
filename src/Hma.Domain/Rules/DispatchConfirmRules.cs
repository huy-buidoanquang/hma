using Hma.Domain.Entities;

namespace Hma.Domain.Rules;

public static class DispatchConfirmRules
{
    public static bool IsPic(int? employeeId, Customer? customer) =>
        IsPic(employeeId, customer?.AccountantEmployeeId);

    public static bool IsPic(int? employeeId, int? accountantEmployeeId) =>
        employeeId is not null && accountantEmployeeId is int pic && employeeId == pic;

    public static bool CanConfirm(int? employeeId, bool isManager, Customer? customer) =>
        CanConfirm(employeeId, isManager, customer?.AccountantEmployeeId);

    public static bool CanConfirm(int? employeeId, bool isManager, int? accountantEmployeeId) =>
        isManager || IsPic(employeeId, accountantEmployeeId);

    public static bool CanUnlock(int? employeeId, bool isManager, Customer? customer) =>
        CanUnlock(employeeId, isManager, customer?.AccountantEmployeeId);

    public static bool CanUnlock(int? employeeId, bool isManager, int? accountantEmployeeId) =>
        CanConfirm(employeeId, isManager, accountantEmployeeId);

    public static bool CanMutateLocked(int? employeeId, bool isManager, Customer? customer) =>
        CanUnlock(employeeId, isManager, customer);

    public static void EnsureCanSave(
        DispatchOrder existing,
        int? employeeId,
        bool isManager,
        Customer? customer)
    {
        if (existing.Status != DispatchStatus.Locked)
            return;
        if (!CanMutateLocked(employeeId, isManager, customer))
            throw new InvalidOperationException(
                "Lệnh đã chốt. Chỉ kế toán phụ trách khách này hoặc quản lý mới được sửa.");
    }

    public static void EnsureCanConfirm(int? employeeId, bool isManager, Customer? customer)
    {
        if (!CanConfirm(employeeId, isManager, customer))
            throw new InvalidOperationException(
                "Chỉ kế toán phụ trách khách hàng này hoặc quản lý mới được chốt lệnh.");
    }

    public static void EnsureCanUnlock(int? employeeId, bool isManager, Customer? customer)
    {
        if (!CanUnlock(employeeId, isManager, customer))
            throw new InvalidOperationException(
                "Chỉ kế toán phụ trách khách hàng này hoặc quản lý mới được bỏ chốt.");
    }
}
