namespace Hma.Application.Features.Customers;

public sealed record CustomerOption(
    int Id,
    string Code,
    string Name,
    string? Address,
    string? Phone,
    string? TaxCode,
    bool IsWalkIn,
    int? AccountantEmployeeId = null);
