namespace Hma.Domain.Entities;

public class CashReceipt : Entity
{
    public string Code { get; set; } = "";
    public DateTime DocumentDate { get; set; } = DateTime.Today;
    public CashReceiptKind Kind { get; set; } = CashReceiptKind.Customer;
    public int? DispatchOrderId { get; set; }
    public DispatchOrder? DispatchOrder { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public decimal Amount { get; set; }
    public string? AmountInWords { get; set; }
    public string? PayerName { get; set; }
    public string? Address { get; set; }
    public string? Reason { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}
