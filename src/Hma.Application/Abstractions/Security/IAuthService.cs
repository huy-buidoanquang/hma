using Hma.Application.Features.Authentication;

namespace Hma.Application.Abstractions.Security;

public interface IAuthService
{
    Task<AuthenticatedUser?> AuthenticateAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default);
}
