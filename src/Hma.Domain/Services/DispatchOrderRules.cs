using Hma.Domain.Entities;

namespace Hma.Domain.Services;

public static class DispatchOrderRules
{
    public static void EnsureCanSave(DispatchOrder order)
    {
        MoneyRules.EnsureNonNegative(order.UnitPrice, "Đơn giá");
        MoneyRules.EnsureNonNegative(order.Surcharge, "Phụ phí");
        MoneyRules.EnsureNonNegative(order.ExtraCost, "Chi phí phát sinh");
        PhoneRules.EnsureOptional(order.SenderPhone, "Số điện thoại người gửi");
        PhoneRules.EnsureOptional(order.ReceiverPhone, "Số điện thoại người nhận");
        TaxCodeRules.EnsureOptional(order.SenderTaxCode, "Mã số thuế người gửi");
        TaxCodeRules.EnsureOptional(order.ReceiverTaxCode, "Mã số thuế người nhận");
    }
}
