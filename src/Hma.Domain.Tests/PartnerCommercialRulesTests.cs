using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class PartnerCommercialRulesTests
{
    [Fact]
    public void Captured_rate_calculates_payable_and_margin()
    {
        var partner = new Partner { Id = 2, Code = "DT01", Name = "Nhà xe A", OperatingFeePercent = 5 };
        var rate = new PartnerRate { Id = 3, UnitPrice = 800_000, Surcharge = 100_000 };
        var order = new DispatchOrder
        {
            TotalAmount = 1_200_000,
            BuyUnitPrice = 800_000,
            BuySurcharge = 100_000
        };

        PartnerCommercialRules.Capture(order, partner, rate, isAssignedPartner: true);

        Assert.Equal(855_000, order.PartnerPayableAmount);
        Assert.Equal(345_000, order.GrossMargin);
        Assert.False(order.IsBuyManual);
        Assert.Equal(3, order.PartnerRateId);
    }

    [Fact]
    public void Manual_buy_price_requires_reason()
    {
        var partner = new Partner { Id = 2, Code = "DT01", Name = "Nhà xe A" };
        var order = new DispatchOrder { BuyUnitPrice = 700_000 };

        Assert.Throws<InvalidOperationException>(() =>
            PartnerCommercialRules.Capture(order, partner, null, isAssignedPartner: true));

        order.BuyOverrideReason = "Giá chuyến phát sinh";
        PartnerCommercialRules.Capture(order, partner, null, isAssignedPartner: true);
        Assert.True(order.IsBuyManual);
    }

    [Fact]
    public void Unassigned_vehicle_does_not_create_partner_payable()
    {
        var order = new DispatchOrder { TotalAmount = 1_000_000, BuyUnitPrice = 800_000 };

        PartnerCommercialRules.Capture(order, null, null, isAssignedPartner: false);

        Assert.Equal(0, order.PartnerPayableAmount);
        Assert.Equal(1_000_000, order.GrossMargin);
    }

    [Fact]
    public void Unassigning_is_blocked_when_approved_partner_exception_cost_exists()
    {
        var order = new DispatchOrder { ApprovedExceptionCost = 100_000 };

        Assert.Throws<InvalidOperationException>(() =>
            PartnerCommercialRules.Capture(order, null, null, isAssignedPartner: false));
    }

    [Fact]
    public void Approved_exception_updates_sell_buy_and_margin_without_changing_base_extra_cost()
    {
        var order = new DispatchOrder
        {
            UnitPrice = 1_000_000,
            ExtraCost = 50_000,
            BuyUnitPrice = 700_000,
            BuyExtraCost = 20_000,
            PartnerOperatingFeePercent = 5
        };
        order.RecalculateTotal();
        order.RecalculatePartnerAmounts();

        order.ApplyApprovedException(120_000, 80_000);

        Assert.Equal(50_000, order.ExtraCost);
        Assert.Equal(20_000, order.BuyExtraCost);
        Assert.Equal(1_170_000, order.TotalAmount);
        Assert.Equal(170_000, order.BillableExtraCost);
        Assert.Equal(800_000, order.BuyTotal);
        Assert.Equal(100_000, order.PartnerBillableExtraCost);
        Assert.Equal(760_000, order.PartnerPayableAmount);
        Assert.Equal(410_000, order.GrossMargin);

        order.ReverseApprovedException(120_000, 80_000);
        Assert.Equal(1_050_000, order.TotalAmount);
        Assert.Equal(720_000, order.BuyTotal);
    }
}
