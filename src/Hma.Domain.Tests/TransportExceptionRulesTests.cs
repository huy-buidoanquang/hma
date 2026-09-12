using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class TransportExceptionRulesTests
{
    [Fact]
    public void Save_requires_active_code_description_and_partner_for_partner_cost()
    {
        var order = new DispatchOrder { Status = DispatchStatus.Completed };
        var item = new TransportException { Description = "Chờ bốc hàng", PartnerCost = 100_000 };
        var code = new TransportExceptionCode { IsActive = true };

        Assert.Throws<InvalidOperationException>(() =>
            TransportExceptionRules.EnsureCanSave(item, order, code));

        order.PartnerId = 2;
        TransportExceptionRules.EnsureCanSave(item, order, code);
        code.IsActive = false;
        Assert.Throws<InvalidOperationException>(() =>
            TransportExceptionRules.EnsureCanSave(item, order, code));
    }

    [Fact]
    public void Review_requires_manager_and_different_user()
    {
        var order = new DispatchOrder { Status = DispatchStatus.Completed };
        var item = new TransportException
        {
            Status = TransportExceptionStatus.Submitted,
            SubmittedByUserId = 10
        };

        Assert.Throws<InvalidOperationException>(() =>
            TransportExceptionRules.EnsureCanReview(item, order, 11, isManager: false));
        Assert.Throws<InvalidOperationException>(() =>
            TransportExceptionRules.EnsureCanReview(item, order, 10, isManager: true));
        TransportExceptionRules.EnsureCanReview(item, order, 11, isManager: true);
    }

    [Fact]
    public void Open_exception_blocks_reconciliation()
    {
        var order = new DispatchOrder();
        order.TransportExceptions.Add(new TransportException { Status = TransportExceptionStatus.Submitted });

        Assert.Throws<InvalidOperationException>(() =>
            TransportExceptionRules.EnsureNoOpenExceptions(order));

        order.TransportExceptions.Single().Status = TransportExceptionStatus.Approved;
        TransportExceptionRules.EnsureNoOpenExceptions(order);
    }

    [Fact]
    public void Approved_partner_cost_locks_partner_assignment()
    {
        var order = new DispatchOrder
        {
            PartnerId = 2,
            ApprovedExceptionCost = 50_000
        };

        TransportExceptionRules.EnsurePartnerAssignmentCanChange(order, 2);
        Assert.Throws<InvalidOperationException>(() =>
            TransportExceptionRules.EnsurePartnerAssignmentCanChange(order, 3));
    }

    [Theory]
    [InlineData(TransportExceptionStatus.Draft, true)]
    [InlineData(TransportExceptionStatus.Rejected, true)]
    [InlineData(TransportExceptionStatus.Submitted, false)]
    [InlineData(TransportExceptionStatus.Approved, false)]
    [InlineData(TransportExceptionStatus.Voided, false)]
    public void Delete_only_allows_draft_or_rejected(
        TransportExceptionStatus status,
        bool isAllowed)
    {
        var item = new TransportException { Status = status };

        if (isAllowed)
            TransportExceptionRules.EnsureCanDelete(item);
        else
            Assert.Throws<InvalidOperationException>(() => TransportExceptionRules.EnsureCanDelete(item));
    }
}
