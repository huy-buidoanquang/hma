using Hma.Domain.Rules;

namespace Hma.Domain.Entities;

public class DispatchOrder : Entity
{
    public string Code { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public DispatchStatus Status { get; set; } = DispatchStatus.Issued;
    public ReconciliationStatus ReconciliationStatus { get; set; } = ReconciliationStatus.Pending;
    public DateTime? ReconciledAt { get; set; }
    public int? ReconciledByUserId { get; set; }
    public AppUser? ReconciledByUser { get; set; }
    public DateTime? ReconciliationSubmittedAt { get; set; }
    public int? ReconciliationSubmittedByUserId { get; set; }
    public AppUser? ReconciliationSubmittedByUser { get; set; }
    public DateTime? ReconciliationRejectedAt { get; set; }
    public int? ReconciliationRejectedByUserId { get; set; }
    public AppUser? ReconciliationRejectedByUser { get; set; }
    public string? ReconciliationRejectionReason { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? SenderCustomerId { get; set; }
    public Customer? SenderCustomer { get; set; }
    public string? SenderName { get; set; }
    public string? SenderPhone { get; set; }
    public string? SenderAddress { get; set; }
    public string? SenderTaxCode { get; set; }
    public int? ReceiverCustomerId { get; set; }
    public Customer? ReceiverCustomer { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ReceiverAddress { get; set; }
    public string? ReceiverTaxCode { get; set; }

    public DateTime PickupAt { get; set; } = DateTime.Now;
    public string? PickupAddress { get; set; }
    public string? DeliveryAddress { get; set; }
    public int? RouteId { get; set; }
    public Route? Route { get; set; }

    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public int? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public int? VehicleTypeId { get; set; }
    public VehicleType? VehicleType { get; set; }

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int? PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public int BillingYear { get; set; }
    public int BillingMonth { get; set; }
    public int? ConfirmedByUserId { get; set; }
    public AppUser? ConfirmedByUser { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? ArNumber { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal Surcharge { get; set; }
    public decimal ExtraCost { get; set; }
    public decimal ApprovedExceptionRevenue { get; set; }
    public decimal TotalAmount { get; set; }
    public int? PriceListItemId { get; set; }
    public PriceListItem? PriceListItem { get; set; }
    public string? PriceSourceSnapshot { get; set; }
    public bool IsFreightManual { get; set; }
    public string? FreightOverrideReason { get; set; }
    public int? PartnerId { get; set; }
    public Partner? Partner { get; set; }
    public string? PartnerNameSnapshot { get; set; }
    public int? PartnerRateId { get; set; }
    public PartnerRate? PartnerRate { get; set; }
    public decimal BuyUnitPrice { get; set; }
    public decimal BuySurcharge { get; set; }
    public decimal BuyExtraCost { get; set; }
    public decimal ApprovedExceptionCost { get; set; }
    public decimal BuyTotal { get; set; }
    public decimal PartnerOperatingFeePercent { get; set; }
    public decimal PartnerPayableAmount { get; set; }
    public decimal GrossMargin { get; set; }
    public string? BuyRateSourceSnapshot { get; set; }
    public bool IsBuyManual { get; set; }
    public string? BuyOverrideReason { get; set; }
    public string? AmountInWords { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<DispatchOrderLine> Lines { get; set; } = new List<DispatchOrderLine>();
    public ICollection<DispatchDocument> Documents { get; set; } = new List<DispatchDocument>();
    public ICollection<DispatchOrderStop> Stops { get; set; } = new List<DispatchOrderStop>();
    public ICollection<TransportException> TransportExceptions { get; set; } = new List<TransportException>();

    public bool HasDeliveryNote => Documents.Any(d => d.Kind == DispatchDocumentKind.DeliveryNote);
    public decimal BillableExtraCost => ExtraCost + ApprovedExceptionRevenue;
    public decimal PartnerBillableExtraCost => BuyExtraCost + ApprovedExceptionCost;

    public string RouteLabel
    {
        get
        {
            var parts = Stops.OrderBy(s => s.Sequence)
                .Select(s => s.NameSnapshot)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            if (parts.Count > 0)
                return string.Join(" → ", parts);
            return Route?.Name ?? "";
        }
    }

    public string PickupLocationName =>
        Stops.OrderBy(s => s.Sequence).Select(s => s.NameSnapshot).FirstOrDefault() ?? "";

    public string DeliveryLocationName =>
        Stops.OrderByDescending(s => s.Sequence).Select(s => s.NameSnapshot).FirstOrDefault() ?? "";

    public void RecalculateTotal()
    {
        TotalAmount = UnitPrice + Surcharge + ExtraCost + ApprovedExceptionRevenue;
    }

    public void RecalculatePartnerAmounts()
    {
        BuyTotal = BuyUnitPrice + BuySurcharge + BuyExtraCost + ApprovedExceptionCost;
        PartnerPayableAmount = PartnerFeeRules.RemainderPayable(BuyTotal, PartnerOperatingFeePercent);
        GrossMargin = TotalAmount - PartnerPayableAmount;
    }

    public void ApplyApprovedException(decimal customerCharge, decimal partnerCost)
    {
        MoneyRules.EnsureNonNegative(customerCharge, "Khoản thu khách hàng");
        MoneyRules.EnsureNonNegative(partnerCost, "Chi phí đối tác");
        ApprovedExceptionRevenue += customerCharge;
        ApprovedExceptionCost += partnerCost;
        RecalculateTotal();
        RecalculatePartnerAmounts();
    }

    public void ReverseApprovedException(decimal customerCharge, decimal partnerCost)
    {
        MoneyRules.EnsureNonNegative(customerCharge, "Khoản thu khách hàng");
        MoneyRules.EnsureNonNegative(partnerCost, "Chi phí đối tác");
        if (customerCharge > ApprovedExceptionRevenue || partnerCost > ApprovedExceptionCost)
            throw new InvalidOperationException("Số tiền hoàn sự cố vượt quá số đã áp dụng vào lệnh.");
        ApprovedExceptionRevenue -= customerCharge;
        ApprovedExceptionCost -= partnerCost;
        RecalculateTotal();
        RecalculatePartnerAmounts();
    }

    public bool CanEdit =>
        !IsDeleted
        && Status is not DispatchStatus.Locked and not DispatchStatus.Cancelled
        && ConfirmedAt is null
        && ReconciliationStatus is ReconciliationStatus.Pending or ReconciliationStatus.Rejected;

    public string CustomerCodeName => Customer is null
        ? ""
        : string.IsNullOrWhiteSpace(Customer.Code) ? Customer.Name : $"{Customer.Code} — {Customer.Name}";

    public void ReplaceStops(IReadOnlyList<(int LocationId, string Name)> stops)
    {
        Stops.Clear();
        var sequence = 0;
        foreach (var stop in stops)
        {
            Stops.Add(new DispatchOrderStop
            {
                Sequence = sequence++,
                LocationId = stop.LocationId,
                NameSnapshot = stop.Name
            });
        }
    }
}
