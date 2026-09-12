using Hma.Application.Features.Customers;
using Hma.Application.Features.TransportExceptions;

namespace Hma.Application.Features.Accounting;

public sealed record CashReceiptDetails(
    int Id,
    string Code,
    DateTime DocumentDate,
    CashReceiptKind Kind,
    int? DispatchOrderId,
    ExceptionOrderOption? DispatchOrder,
    int? CustomerId,
    CustomerOption? Customer,
    decimal Amount,
    string? AmountInWords,
    string? PayerName,
    string? Address,
    string? Reason,
    int? EmployeeId);
