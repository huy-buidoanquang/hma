using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class PriceListDomainServiceTests
{
    [Fact]
    public void Requires_code_and_name()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PriceListDomainService.EnsureCanSave(new PriceList { Code = "", Name = "A" }));
    }

    [Fact]
    public void Locked_price_list_cannot_be_modified()
    {
        var list = new PriceList { IsLocked = true };

        Assert.Throws<InvalidOperationException>(() =>
            PriceListDomainService.EnsureCanModify(list));
    }

    [Fact]
    public void Lock_requires_reason()
    {
        var list = new PriceList { LockReason = " " };

        Assert.Throws<InvalidOperationException>(() =>
            PriceListDomainService.EnsureCanLock(list));

        list.LockReason = "Áp dụng từ tháng 9";
        PriceListDomainService.EnsureCanLock(list);
    }

    [Fact]
    public void Rejects_effective_to_before_from()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PriceListDomainService.EnsureCanSave(new PriceList
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
        PriceListDomainService.EnsureCanSave(new PriceList
        {
            Code = "BG1",
            Name = "Bảng A",
            EffectiveFrom = new DateTime(2026, 1, 1)
        });
    }
}
