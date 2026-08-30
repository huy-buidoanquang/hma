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
    public int? PickupCityId { get; set; }
    public City? PickupCity { get; set; }
    public string? DeliveryAddress { get; set; }
    public int? DeliveryCityId { get; set; }
    public City? DeliveryCity { get; set; }

    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public int? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public int? VehicleTypeId { get; set; }
    public VehicleType? VehicleType { get; set; }

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal Surcharge { get; set; }
    public decimal ExtraCost { get; set; }
    public decimal TotalAmount { get; set; }
    public string? AmountInWords { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<DispatchOrderLine> Lines { get; set; } = new List<DispatchOrderLine>();
    public ICollection<DispatchDocument> Documents { get; set; } = new List<DispatchDocument>();

    public bool HasDeliveryNote => Documents.Any(d => d.Kind == DispatchDocumentKind.DeliveryNote);

    public string RouteLabel
    {
        get
        {
            var from = PickupCity?.Name ?? PickupAddress ?? "";
            var to = DeliveryCity?.Name ?? DeliveryAddress ?? "";
            if (string.IsNullOrWhiteSpace(from) && string.IsNullOrWhiteSpace(to)) return "";
            return $"{from} → {to}";
        }
    }

    public void RecalculateTotal()
    {
        TotalAmount = UnitPrice + Surcharge + ExtraCost;
    }

    public bool CanEdit =>
        !IsDeleted && Status is not DispatchStatus.Locked && ReconciliationStatus != ReconciliationStatus.Reconciled;
}
