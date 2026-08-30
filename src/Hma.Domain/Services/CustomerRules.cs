using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class CustomerRules
{
    public static void EnsureCanSave(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Code) || string.IsNullOrWhiteSpace(customer.Name))
            throw new InvalidOperationException("Mã và tên khách hàng là bắt buộc.");
        PhoneRules.EnsureOptional(customer.Phone);
        EmailRules.EnsureOptional(customer.Email);
        TaxCodeRules.EnsureOptional(customer.TaxCode);
    }
}
