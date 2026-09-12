using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Customers;

namespace Hma.Application.Features.Accounting;

public sealed record CashPaymentDetails(
    int Id,
    string Code,
    DateTime DocumentDate,
    CashPaymentKind Kind,
    int? CustomerId,
    CustomerOption? Customer,
    int? DriverEmployeeId,
    EmployeeOption? DriverEmployee,
    decimal Amount,
    string? AmountInWords,
    string? PayeeName,
    string? Address,
    string? Reason,
    int? EmployeeId);
