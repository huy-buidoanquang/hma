namespace Hma.Application.Features.Catalogs;

public sealed record SaveEmployeeCommand(
    int Id,
    string Code,
    string Name,
    string? Address,
    string? Phone,
    string? Mobile,
    DateTime? BirthDate,
    string? IdentityNumber,
    string? VehiclePlate,
    int? DepartmentId,
    int? JobTitleId);
