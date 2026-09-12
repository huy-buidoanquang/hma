namespace Hma.Application.Features.Catalogs;

public sealed record DriverOption(int Id, string Code, string Name, string? Phone, int PartnerId, PartnerOption? Partner);
