using Hma.Application.Features.Catalogs;

namespace Hma.Application.Features.Customers;

public sealed record CustomerSummary(
    int Id,
    string Code,
    string Name,
    string? Address,
    string? Phone,
    string? TaxCode,
    string? ContactName,
    string? Email,
    int? CityId,
    CatalogOption? City,
    int? AccountantEmployeeId,
    EmployeeOption? AccountantEmployee,
    bool IsWalkIn,
    DateTime? UpdatedAt)
{
    public string CodeName => string.IsNullOrWhiteSpace(Code) ? Name : $"{Code} — {Name}";
}
