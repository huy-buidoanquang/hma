using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Authentication;

namespace Hma.Desktop.Wpf.Abstractions;

public interface IDesktopUserSession : ICurrentUser
{
    void SignIn(AuthenticatedUser user);
    void SignOut();
}
