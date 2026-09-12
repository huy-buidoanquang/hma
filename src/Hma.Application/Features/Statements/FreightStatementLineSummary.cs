namespace Hma.Application.Features.Statements;

public sealed record FreightStatementLineSummary(
    int Id,
    DateTime TripDate,
    string DispatchCode,
    string? Route,
    string? PlateNumber,
    string? Tonnage,
    string? DriverName,
    decimal UnitPrice,
    decimal Surcharge,
    decimal ExtraCost,
    decimal LineTotal,
    string? Notes);
