using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class CustomerAliasRulesTests
{
    [Fact]
    public void Rejects_blank_alias_or_customer()
    {
        var blank = Assert.Throws<InvalidOperationException>(() =>
            CustomerAliasRules.EnsureCanSave(" ", 1));
        Assert.Contains("bắt buộc", blank.Message, StringComparison.Ordinal);

        var customer = Assert.Throws<InvalidOperationException>(() =>
            CustomerAliasRules.EnsureCanSave("TEC", 0));
        Assert.Contains("khách hàng đích", customer.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unique_is_case_insensitive()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomerAliasRules.EnsureUnique("tec", ["TEC"]));
        Assert.Contains("đã tồn tại", ex.Message, StringComparison.Ordinal);
        CustomerAliasRules.EnsureUnique("KMG", ["TEC"]);
    }

    [Fact]
    public void Rejects_alias_equal_to_customer_code_or_name()
    {
        var customers = new[]
        {
            new Customer { Code = "KH001", Name = "Thép" },
            new Customer { Code = "KH002", Name = "TEC Logistics" }
        };
        var code = Assert.Throws<InvalidOperationException>(() =>
            CustomerAliasRules.EnsureNotCustomerCatalog("kh001", customers));
        Assert.Contains("khách hàng", code.Message, StringComparison.Ordinal);
        var name = Assert.Throws<InvalidOperationException>(() =>
            CustomerAliasRules.EnsureNotCustomerCatalog("Thép", customers));
        Assert.Contains("khách hàng", name.Message, StringComparison.Ordinal);
        CustomerAliasRules.EnsureNotCustomerCatalog("TEC", customers);
    }
}
