namespace Hma.Application.Features.PartnerSettlements;

public sealed record PartnerSettlementLineSummary(
    int Id,
    DateTime TripDate,
    string DispatchCode,
    string? Route,
    string? PlateNumber,
    string? DriverName,
    decimal BuyTotal,
    decimal OperatingFeePercent,
    decimal OperatingFeeAmount,
    decimal PayableAmount);
