using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class PartnerRules
{
    public static void EnsureCanSave(Partner partner)
    {
        if (string.IsNullOrWhiteSpace(partner.Code) || string.IsNullOrWhiteSpace(partner.Name))
            throw new InvalidOperationException("Mã và tên đối tác là bắt buộc.");
        PhoneRules.EnsureOptional(partner.Phone);
        EmailRules.EnsureOptional(partner.Email);
        TaxCodeRules.EnsureOptional(partner.TaxCode);
    }
}
