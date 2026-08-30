namespace Hma.Domain.Entities;

public class VatInvoiceLine : Entity
{
    public int VatInvoiceId { get; set; }
    public VatInvoice? VatInvoice { get; set; }
    public int DispatchOrderId { get; set; }
    public DispatchOrder? DispatchOrder { get; set; }
}
