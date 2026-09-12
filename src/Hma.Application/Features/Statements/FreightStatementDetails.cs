using Hma.Application.Features.Customers;

namespace Hma.Application.Features.Statements;

public sealed record FreightStatementDetails(
    int Id,
    string Code,
    int CustomerId,
    CustomerOption? Customer,
    int Year,
    int Month,
    FinancialDocumentStatus Status,
    int? SubmittedByUserId,
    string? VoidReason,
    int TripCount,
    decimal FreightTotal,
    decimal SurchargeTotal,
    decimal ExtraCostTotal,
    decimal GrandTotal,
    decimal VatRate,
    decimal VatAmount,
    decimal TotalWithVat,
    string? Notes,
    IReadOnlyList<FreightStatementLineSummary> Lines);
