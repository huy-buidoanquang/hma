using Hma.Domain.Entities;

namespace Hma.Domain.Rules;

public static class DispatchOrderRules
{
    public static void EnsureCanSave(DispatchOrder order)
    {
        if (order.CustomerId is null)
            throw new InvalidOperationException("Cần chọn khách hàng theo mã đã tạo.");
        if (order.RouteId is null or <= 0)
            throw new InvalidOperationException("Cần chọn tuyến.");
        if (order.Stops.Count < 2)
            throw new InvalidOperationException("Tuyến cần ít nhất hai điểm.");
        if (order.VehicleId is null)
            throw new InvalidOperationException("Cần chọn biển kiểm soát.");
        if (order.DriverId is null)
            throw new InvalidOperationException("Cần chọn tài xế.");
        MoneyRules.EnsureNonNegative(order.UnitPrice, "Cước");
        MoneyRules.EnsureNonNegative(order.Surcharge, "Phụ phí");
        MoneyRules.EnsureNonNegative(order.ExtraCost, "Chi phí phát sinh");
        PhoneRules.EnsureOptional(order.SenderPhone, "Số điện thoại người gửi");
        PhoneRules.EnsureOptional(order.ReceiverPhone, "Số điện thoại người nhận");
        TaxCodeRules.EnsureOptional(order.SenderTaxCode, "Mã số thuế người gửi");
        TaxCodeRules.EnsureOptional(order.ReceiverTaxCode, "Mã số thuế người nhận");
        BillingPeriodRules.ApplyDefault(order);
    }
}
