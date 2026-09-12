using Hma.Application.Features.Accounting;

namespace Hma.Desktop.Wpf.Presentation.Features.Accounting.Models;

public sealed record InvoiceOrderModel(int Id, string Code, decimal TotalAmount, Hma.Application.Features.Customers.CustomerOption? SenderCustomer)
{
    public static InvoiceOrderModel From(InvoiceOrderOption item) => new(
        item.Id, item.Code, item.TotalAmount, item.SenderCustomer);
}
