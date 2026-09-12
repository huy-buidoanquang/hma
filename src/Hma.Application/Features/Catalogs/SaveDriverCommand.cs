namespace Hma.Application.Features.Catalogs;

public sealed record SaveDriverCommand(
    int Id,
    string Code,
    string Name,
    string? Phone,
    DateTime? BirthDate,
    string? IdentityNumber,
    int PartnerId);
