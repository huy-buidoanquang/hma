namespace Hma.Application.Features.Catalogs;

public sealed record SavePartnerCommand(
    int Id,
    string Code,
    string Name,
    string? TaxCode,
    string? Address,
    string? ContactName,
    string? Phone,
    string? Email,
    decimal OperatingFeePercent);
