using Hma.Application.Features.Catalogs;

namespace Hma.Application.Features.PartnerSettlements;

public sealed record PartnerSettlementDetails(
    int Id,
    string Code,
    int PartnerId,
    PartnerOption? Partner,
    int Year,
    int Month,
    FinancialDocumentStatus Status,
    int? SubmittedByUserId,
    string? VoidReason,
    int TripCount,
    decimal GrossAmount,
    decimal OperatingFeeAmount,
    decimal PayableAmount,
    string? Notes,
    IReadOnlyList<PartnerSettlementLineSummary> Lines);
