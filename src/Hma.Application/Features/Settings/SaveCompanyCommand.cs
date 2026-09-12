namespace Hma.Application.Features.Settings;

public sealed record SaveCompanyCommand(
    int Id,
    string Name,
    string? Address,
    string? Phone,
    string? TaxCode,
    string? Bank,
    string? Website,
    string? Email);
