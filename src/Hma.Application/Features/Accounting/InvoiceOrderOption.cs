using Hma.Application.Features.Customers;

namespace Hma.Application.Features.Accounting;

public sealed record InvoiceOrderOption(
    int Id,
    string Code,
    decimal TotalAmount,
    CustomerOption? SenderCustomer);
