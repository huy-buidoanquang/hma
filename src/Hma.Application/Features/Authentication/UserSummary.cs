using Hma.Application.Features.Catalogs;

namespace Hma.Application.Features.Authentication;

public sealed record UserSummary(
    int Id,
    string UserName,
    string? DisplayName,
    int? EmployeeId,
    EmployeeOption? Employee,
    bool IsManager,
    bool IsSpecial,
    IReadOnlyList<UserPermissionDetails> Permissions);
