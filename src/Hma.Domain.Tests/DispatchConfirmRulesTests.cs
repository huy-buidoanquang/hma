using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class DispatchConfirmRulesTests
{
    private static readonly Customer PicCustomer = new() { AccountantEmployeeId = 7 };
    [Fact]
    public void IsPic_matches_customer_accountant_employee()
    {
        Assert.True(DispatchConfirmRules.IsPic(7, PicCustomer));
        Assert.False(DispatchConfirmRules.IsPic(8, PicCustomer));
        Assert.False(DispatchConfirmRules.IsPic(7, new Customer()));
        Assert.False(DispatchConfirmRules.IsPic(null, PicCustomer));
    }

    [Fact]
    public void CanConfirm_allows_pic_or_manager_only()
    {
        Assert.True(DispatchConfirmRules.CanConfirm(7, false, PicCustomer));
        Assert.True(DispatchConfirmRules.CanConfirm(1, true, PicCustomer));
        Assert.False(DispatchConfirmRules.CanConfirm(8, false, PicCustomer));
        Assert.False(DispatchConfirmRules.CanConfirm(7, false, new Customer { AccountantEmployeeId = 99 }));
    }

    [Fact]
    public void EnsureCanSave_blocks_other_accountant_when_locked()
    {
        var locked = new DispatchOrder { Status = DispatchStatus.Locked };
        DispatchConfirmRules.EnsureCanSave(locked, 7, false, PicCustomer);
        DispatchConfirmRules.EnsureCanSave(locked, 1, true, PicCustomer);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DispatchConfirmRules.EnsureCanSave(locked, 8, false, PicCustomer));
        Assert.Contains("đã chốt", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureCanSave_allows_anyone_when_unlocked()
    {
        var issued = new DispatchOrder { Status = DispatchStatus.Issued };
        DispatchConfirmRules.EnsureCanSave(issued, 8, false, PicCustomer);
        DispatchConfirmRules.EnsureCanSave(issued, null, false, PicCustomer);
    }

    [Fact]
    public void Unlock_same_people_as_confirm()
    {
        DispatchConfirmRules.EnsureCanUnlock(7, false, PicCustomer);
        DispatchConfirmRules.EnsureCanUnlock(1, true, PicCustomer);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DispatchConfirmRules.EnsureCanUnlock(8, false, PicCustomer));
        Assert.Contains("bỏ chốt", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_rejects_other_accountant()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DispatchConfirmRules.EnsureCanConfirm(8, false, PicCustomer));
        Assert.Contains("phụ trách", ex.Message, StringComparison.Ordinal);
    }
}
