namespace Hma.Domain.Entities;

public class CashPayment : Entity
{
    public string Code { get; set; } = "";
    public DateTime DocumentDate { get; set; } = DateTime.Today;
    public CashPaymentKind Kind { get; set; } = CashPaymentKind.Customer;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? DriverEmployeeId { get; set; }
    public Employee? DriverEmployee { get; set; }
    public decimal Amount { get; set; }
    public string? AmountInWords { get; set; }
    public string? PayeeName { get; set; }
    public string? Address { get; set; }
    public string? Reason { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}
