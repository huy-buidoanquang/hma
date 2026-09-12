namespace Hma.Application.Features.Settings;

public sealed record CompanyDetails(
    int Id,
    string Name,
    string? Address,
    string? Phone,
    string? TaxCode,
    string? Bank,
    string? Website,
    string? Email);
