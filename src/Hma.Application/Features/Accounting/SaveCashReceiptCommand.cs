namespace Hma.Application.Features.Accounting;

public sealed record SaveCashReceiptCommand(
    int Id,
    string Code,
    DateTime DocumentDate,
    CashReceiptKind Kind,
    int? DispatchOrderId,
    int? CustomerId,
    decimal Amount,
    string? AmountInWords,
    string? PayerName,
    string? Address,
    string? Reason,
    int? EmployeeId);
