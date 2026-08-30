namespace Hma.Domain.Entities;

public class VatInvoice : Entity
{
    public string Code { get; set; } = "";
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public bool IsByCustomer { get; set; }
    public string? GoodsName { get; set; }
    public string? PaymentMethodText { get; set; }
    public string? Unit { get; set; }
    public decimal? Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal VatRate { get; set; } = 10;
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? AmountInWords { get; set; }
    public ICollection<VatInvoiceLine> Lines { get; set; } = new List<VatInvoiceLine>();

    public void RecalculateFromTotal()
    {
        if (VatRate <= -100) return;
        Amount = Math.Round(TotalAmount / (1 + VatRate / 100m), 0);
        VatAmount = TotalAmount - Amount;
    }
}
