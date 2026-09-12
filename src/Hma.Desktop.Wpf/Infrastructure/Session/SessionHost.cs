using Hma.Desktop.Wpf.Abstractions;

namespace Hma.Desktop.Wpf.Infrastructure.Session;

public sealed class SessionHost : ISessionHost
{
    public Action? SignOutHandler { get; set; }

    public void SignOut() => SignOutHandler?.Invoke();
}
