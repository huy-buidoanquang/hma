using Hma.Domain.Entities;

namespace Hma.Application.Abstractions;

public interface IAuthService
{
    Task<AppUser?> LoginAsync(string userName, string password, CancellationToken cancellationToken = default);
    void Logout();
}
