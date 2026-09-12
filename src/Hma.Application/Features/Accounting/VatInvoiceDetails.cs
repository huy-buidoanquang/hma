using Hma.Application.Features.Customers;

namespace Hma.Application.Features.Accounting;

public sealed record VatInvoiceDetails(
    int Id,
    string Code,
    DateTime InvoiceDate,
    int? CustomerId,
    CustomerOption? Customer,
    int? EmployeeId,
    bool IsByCustomer,
    string? GoodsName,
    string? PaymentMethodText,
    string? Unit,
    decimal? Quantity,
    decimal Amount,
    decimal VatRate,
    decimal VatAmount,
    decimal TotalAmount,
    string? AmountInWords,
    IReadOnlyList<InvoiceOrderOption> Lines);
