using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class PartnerSettlementRulesTests
{
    [Fact]
    public void Eligibility_requires_completed_pod_and_matching_partner()
    {
        var valid = Order(2, DispatchStatus.Completed, hasPod: true);
        PartnerSettlementRules.EnsureOrderEligible(valid, 2);

        Assert.Throws<InvalidOperationException>(() =>
            PartnerSettlementRules.EnsureOrderEligible(Order(2, DispatchStatus.Issued, true), 2));
        Assert.Throws<InvalidOperationException>(() =>
            PartnerSettlementRules.EnsureOrderEligible(Order(2, DispatchStatus.Completed, false), 2));
        Assert.Throws<InvalidOperationException>(() =>
            PartnerSettlementRules.EnsureOrderEligible(valid, 3));
    }

    [Fact]
    public void Finalize_enforces_maker_checker()
    {
        var settlement = new PartnerSettlement
        {
            Status = FinancialDocumentStatus.Submitted,
            SubmittedByUserId = 10
        };

        Assert.Throws<InvalidOperationException>(() =>
            PartnerSettlementRules.EnsureCanFinalize(settlement, 10));
        PartnerSettlementRules.EnsureCanFinalize(settlement, 11);
    }

    private static DispatchOrder Order(int partnerId, DispatchStatus status, bool hasPod)
    {
        var order = new DispatchOrder { PartnerId = partnerId, Status = status };
        if (hasPod)
            order.Documents.Add(new DispatchDocument { Kind = DispatchDocumentKind.DeliveryNote });
        return order;
    }
}
