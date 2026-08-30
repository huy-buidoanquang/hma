using Hma.Domain.Entities;

namespace Hma.Application.Abstractions;

public interface ICurrentUser
{
    AppUser? User { get; set; }
    bool IsAuthenticated => User is not null;
    bool Can(string screenKey, PermissionAction action);
}
