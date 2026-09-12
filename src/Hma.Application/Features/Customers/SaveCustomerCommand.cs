namespace Hma.Application.Features.Customers;

public sealed record SaveCustomerCommand(
    int Id,
    string Code,
    string Name,
    string? Address,
    string? Phone,
    string? TaxCode,
    string? ContactName,
    string? Email,
    int? CityId,
    int? AccountantEmployeeId,
    bool IsWalkIn);
