using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class DispatchConfirmRules
{
    public static bool IsPic(AppUser? user, Customer? customer) =>
        user is not null
        && customer?.AccountantEmployeeId is int pic
        && user.EmployeeId == pic;

    public static bool CanConfirm(AppUser? user, Customer? customer) =>
        user?.IsManager == true || IsPic(user, customer);

    public static bool CanUnlock(AppUser? user, Customer? customer) => CanConfirm(user, customer);

    public static bool CanMutateLocked(AppUser? user, Customer? customer) => CanUnlock(user, customer);

    public static void EnsureCanSave(DispatchOrder existing, AppUser? user, Customer? customer)
    {
        if (existing.Status != DispatchStatus.Locked)
            return;
        if (!CanMutateLocked(user, customer))
            throw new InvalidOperationException(
                "Lệnh đã chốt. Chỉ kế toán phụ trách khách này hoặc quản lý mới được sửa.");
    }

    public static void EnsureCanConfirm(AppUser? user, Customer? customer)
    {
        if (!CanConfirm(user, customer))
            throw new InvalidOperationException(
                "Chỉ kế toán phụ trách khách hàng này hoặc quản lý mới được chốt lệnh.");
    }

    public static void EnsureCanUnlock(AppUser? user, Customer? customer)
    {
        if (!CanUnlock(user, customer))
            throw new InvalidOperationException(
                "Chỉ kế toán phụ trách khách hàng này hoặc quản lý mới được bỏ chốt.");
    }
}
