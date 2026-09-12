using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Customers;

namespace Hma.Application.Features.Dispatching;

public sealed record DispatchOrderSummary
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public DateTime PickupAt { get; init; }
    public DispatchStatus Status { get; init; }
    public ReconciliationStatus ReconciliationStatus { get; init; }
    public CustomerOption? Customer { get; init; }
    public int? RouteId { get; init; }
    public DriverOption? Driver { get; init; }
    public int? DriverId { get; init; }
    public VehicleOption? Vehicle { get; init; }
    public int? VehicleId { get; init; }
    public VehicleTypeOption? VehicleType { get; init; }
    public PaymentMethodOption? PaymentMethod { get; init; }
    public string RouteLabel { get; init; } = "";
    public decimal UnitPrice { get; init; }
    public decimal Surcharge { get; init; }
    public decimal ExtraCost { get; init; }
    public decimal ApprovedExceptionRevenue { get; init; }
    public decimal TotalAmount { get; init; }
    public int BillingYear { get; init; }
    public int BillingMonth { get; init; }
    public string? Notes { get; init; }
    public string? SenderName { get; init; }
    public string? PartnerNameSnapshot { get; init; }
    public decimal PartnerOperatingFeePercent { get; init; }
    public decimal PartnerPayableAmount { get; init; }
    public string? ArNumber { get; init; }
    public bool HasDeliveryNote { get; init; }
    public int? ReconciliationSubmittedByUserId { get; init; }
    public string? ReconciliationRejectionReason { get; init; }
    public UserOption? CreatedByUser { get; init; }
    public bool CanEdit { get; init; }
    public decimal BillableExtraCost => ExtraCost + ApprovedExceptionRevenue;
    public string CustomerCodeName => Customer is null ? "" : $"{Customer.Code} - {Customer.Name}";
}
