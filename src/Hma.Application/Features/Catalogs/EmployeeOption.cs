namespace Hma.Application.Features.Catalogs;

public sealed record EmployeeOption(int Id, string Code, string Name, int? DepartmentId, CatalogOption? Department, int? JobTitleId, CatalogOption? JobTitle);
