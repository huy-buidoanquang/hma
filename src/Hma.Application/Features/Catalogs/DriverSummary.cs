namespace Hma.Application.Features.Catalogs;

public sealed record DriverSummary(
    int Id,
    string Code,
    string Name,
    string? Phone,
    DateTime? BirthDate,
    string? IdentityNumber,
    int PartnerId,
    PartnerOption? Partner);
