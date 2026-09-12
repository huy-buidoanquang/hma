namespace Hma.Application.Features.Authentication;

public sealed record SaveUserCommand(
    int Id,
    string UserName,
    string? DisplayName,
    int? EmployeeId,
    bool IsManager,
    bool IsSpecial,
    string? NewPassword,
    IReadOnlyList<UserPermissionDetails> Permissions);
