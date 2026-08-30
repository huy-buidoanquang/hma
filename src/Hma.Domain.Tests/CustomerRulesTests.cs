using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class CustomerRulesTests
{
    [Fact]
    public void Requires_code_and_name()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomerRules.EnsureCanSave(new Customer { Code = " ", Name = "A" }));
        Assert.Contains("bắt buộc", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_invalid_phone_and_email()
    {
        var customer = new Customer
        {
            Code = "KH01",
            Name = "Công ty A",
            Phone = "que",
            Email = "not-an-email"
        };
        Assert.Throws<InvalidOperationException>(() => CustomerRules.EnsureCanSave(customer));
    }

    [Fact]
    public void Accepts_valid_contact_fields()
    {
        CustomerRules.EnsureCanSave(new Customer
        {
            Code = "KH01",
            Name = "Công ty A",
            Phone = "0901234567",
            Email = "a@b.com",
            TaxCode = "0304123456"
        });
    }
}
