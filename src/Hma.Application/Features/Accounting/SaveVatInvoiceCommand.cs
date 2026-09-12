namespace Hma.Application.Features.Accounting;

public sealed record SaveVatInvoiceCommand(
    int Id,
    string Code,
    DateTime InvoiceDate,
    int? CustomerId,
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
    IReadOnlyList<int> DispatchOrderIds);
