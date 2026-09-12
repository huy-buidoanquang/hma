namespace Hma.Application.Features.Accounting;

public sealed record SaveCashPaymentCommand(
    int Id,
    string Code,
    DateTime DocumentDate,
    CashPaymentKind Kind,
    int? CustomerId,
    int? DriverEmployeeId,
    decimal Amount,
    string? AmountInWords,
    string? PayeeName,
    string? Address,
    string? Reason,
    int? EmployeeId);
