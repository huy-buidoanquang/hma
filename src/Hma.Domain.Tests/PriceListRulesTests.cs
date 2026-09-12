using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class PriceListRulesTests
{
    [Fact]
    public void Requires_code_and_name()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PriceListRules.EnsureCanSave(new PriceList { Code = "", Name = "A" }));
    }

    [Fact]
    public void Locked_price_list_cannot_be_modified()
    {
        var list = new PriceList { IsLocked = true };

        Assert.Throws<InvalidOperationException>(() =>
            PriceListRules.EnsureCanModify(list));
    }

    [Fact]
    public void Lock_requires_reason()
    {
        var list = new PriceList { LockReason = " " };

        Assert.Throws<InvalidOperationException>(() =>
            PriceListRules.EnsureCanLock(list));

        list.LockReason = "Áp dụng từ tháng 9";
        PriceListRules.EnsureCanLock(list);
    }

    [Fact]
    public void Rejects_effective_to_before_from()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PriceListRules.EnsureCanSave(new PriceList
            {
                Code = "BG1",
                Name = "Bảng A",
                EffectiveFrom = new DateTime(2026, 2, 1),
                EffectiveTo = new DateTime(2026, 1, 1)
            }));
        Assert.Contains("hiệu lực", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Accepts_open_ended_period()
    {
        PriceListRules.EnsureCanSave(new PriceList
        {
            Code = "BG1",
            Name = "Bảng A",
            EffectiveFrom = new DateTime(2026, 1, 1)
        });
    }
}
