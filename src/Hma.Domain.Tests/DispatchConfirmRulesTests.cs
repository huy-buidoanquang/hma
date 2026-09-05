using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class DispatchConfirmRulesTests
{
    private static readonly Customer PicCustomer = new() { AccountantEmployeeId = 7 };
    private static readonly AppUser Pic = new() { EmployeeId = 7, IsManager = false };
    private static readonly AppUser OtherAccountant = new() { EmployeeId = 8, IsManager = false };
    private static readonly AppUser Manager = new() { EmployeeId = 1, IsManager = true };

    [Fact]
    public void IsPic_matches_customer_accountant_employee()
    {
        Assert.True(DispatchConfirmRules.IsPic(Pic, PicCustomer));
        Assert.False(DispatchConfirmRules.IsPic(OtherAccountant, PicCustomer));
        Assert.False(DispatchConfirmRules.IsPic(Pic, new Customer()));
        Assert.False(DispatchConfirmRules.IsPic(null, PicCustomer));
    }

    [Fact]
    public void CanConfirm_allows_pic_or_manager_only()
    {
        Assert.True(DispatchConfirmRules.CanConfirm(Pic, PicCustomer));
        Assert.True(DispatchConfirmRules.CanConfirm(Manager, PicCustomer));
        Assert.False(DispatchConfirmRules.CanConfirm(OtherAccountant, PicCustomer));
        Assert.False(DispatchConfirmRules.CanConfirm(Pic, new Customer { AccountantEmployeeId = 99 }));
    }

    [Fact]
    public void EnsureCanSave_blocks_other_accountant_when_locked()
    {
        var locked = new DispatchOrder { Status = DispatchStatus.Locked };
        DispatchConfirmRules.EnsureCanSave(locked, Pic, PicCustomer);
        DispatchConfirmRules.EnsureCanSave(locked, Manager, PicCustomer);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DispatchConfirmRules.EnsureCanSave(locked, OtherAccountant, PicCustomer));
        Assert.Contains("đã chốt", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureCanSave_allows_anyone_when_unlocked()
    {
        var issued = new DispatchOrder { Status = DispatchStatus.Issued };
        DispatchConfirmRules.EnsureCanSave(issued, OtherAccountant, PicCustomer);
        DispatchConfirmRules.EnsureCanSave(issued, null, PicCustomer);
    }

    [Fact]
    public void Unlock_same_people_as_confirm()
    {
        DispatchConfirmRules.EnsureCanUnlock(Pic, PicCustomer);
        DispatchConfirmRules.EnsureCanUnlock(Manager, PicCustomer);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DispatchConfirmRules.EnsureCanUnlock(OtherAccountant, PicCustomer));
        Assert.Contains("bỏ chốt", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_rejects_other_accountant()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DispatchConfirmRules.EnsureCanConfirm(OtherAccountant, PicCustomer));
        Assert.Contains("phụ trách", ex.Message, StringComparison.Ordinal);
    }
}
