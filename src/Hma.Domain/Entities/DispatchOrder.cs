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
    public decimal TotalAmount { get; set; }
    public string? AmountInWords { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<DispatchOrderLine> Lines { get; set; } = new List<DispatchOrderLine>();
    public ICollection<DispatchDocument> Documents { get; set; } = new List<DispatchDocument>();
    public ICollection<DispatchOrderStop> Stops { get; set; } = new List<DispatchOrderStop>();

    public bool HasDeliveryNote => Documents.Any(d => d.Kind == DispatchDocumentKind.DeliveryNote);

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
        TotalAmount = UnitPrice + Surcharge + ExtraCost;
    }

    public bool CanEdit =>
        !IsDeleted && Status is not DispatchStatus.Locked && ReconciliationStatus != ReconciliationStatus.Reconciled;

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
