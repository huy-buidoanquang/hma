using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class CustomerAliasRules
{
    public static void EnsureCanSave(string? alias, int customerId)
    {
        var text = AliasText.Normalize(alias);
        if (text.Length == 0)
            throw new InvalidOperationException("Bí danh khách hàng là bắt buộc.");
        if (text.Length > AliasText.MaxLength)
            throw new InvalidOperationException($"Bí danh tối đa {AliasText.MaxLength} ký tự.");
        if (customerId <= 0)
            throw new InvalidOperationException("Cần chọn khách hàng đích.");
    }

    public static void EnsureUnique(string alias, IEnumerable<string> otherAliases)
    {
        if (otherAliases.Any(existing => AliasText.EqualsNormalized(existing, alias)))
            throw new InvalidOperationException("Bí danh khách hàng này đã tồn tại.");
    }

    public static void EnsureNotCustomerCatalog(string alias, IEnumerable<Customer> customers)
    {
        foreach (var customer in customers)
        {
            if (AliasText.EqualsNormalized(customer.Code, alias) || AliasText.EqualsNormalized(customer.Name, alias))
                throw new InvalidOperationException("Bí danh trùng mã hoặc tên khách hàng — dùng catalog Khách hàng, không cần thêm bí danh.");
        }
    }
}
