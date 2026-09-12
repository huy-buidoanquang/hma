using Hma.Domain.Entities;

namespace Hma.Domain.Rules;

public static class PartnerCommercialRules
{
    public static void Capture(
        DispatchOrder order,
        Partner? partner,
        PartnerRate? rate,
        bool isAssignedPartner)
    {
        if (!isAssignedPartner || partner is null)
        {
            if (order.ApprovedExceptionCost > 0)
                throw new InvalidOperationException(
                    "Không thể bỏ nhà xe khi lệnh đã có chi phí sự cố được duyệt cho đối tác.");
            order.PartnerId = null;
            order.PartnerNameSnapshot = null;
            order.PartnerRateId = null;
            order.BuyUnitPrice = 0;
            order.BuySurcharge = 0;
            order.BuyExtraCost = 0;
            order.PartnerOperatingFeePercent = 0;
            order.BuyRateSourceSnapshot = null;
            order.IsBuyManual = false;
            order.BuyOverrideReason = null;
            order.RecalculatePartnerAmounts();
            return;
        }

        MoneyRules.EnsureNonNegative(order.BuyUnitPrice, "Giá mua");
        MoneyRules.EnsureNonNegative(order.BuySurcharge, "Phụ phí mua");
        MoneyRules.EnsureNonNegative(order.BuyExtraCost, "Phát sinh mua");
        PartnerFeeRules.EnsurePercent(partner.OperatingFeePercent);

        order.PartnerId = partner.Id;
        order.PartnerNameSnapshot = partner.Name;
        order.PartnerOperatingFeePercent = partner.OperatingFeePercent;
        var matchesRate = rate is not null
                          && order.BuyUnitPrice == rate.UnitPrice
                          && order.BuySurcharge == rate.Surcharge;
        if (matchesRate)
        {
            order.PartnerRateId = rate!.Id;
            order.BuyRateSourceSnapshot = $"{partner.Code} · {rate.Route?.Name ?? ""} · {rate.UnitPrice:N0}";
            order.IsBuyManual = false;
            order.BuyOverrideReason = null;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(order.BuyOverrideReason))
                throw new InvalidOperationException("Cần nhập lý do khi dùng giá mua thủ công hoặc khác bảng giá đối tác.");
            order.PartnerRateId = rate?.Id;
            order.BuyRateSourceSnapshot = rate is null
                ? null
                : $"{partner.Code} · {rate.Route?.Name ?? ""} · {rate.UnitPrice:N0}";
            order.IsBuyManual = true;
            order.BuyOverrideReason = order.BuyOverrideReason.Trim();
        }

        order.RecalculatePartnerAmounts();
    }
}
