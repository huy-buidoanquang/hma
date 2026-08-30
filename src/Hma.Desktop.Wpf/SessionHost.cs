using Hma.Desktop.Wpf.ViewModels;

namespace Hma.Desktop.Wpf;

public sealed class SessionHost : ISessionHost
{
    public Action? SignOutHandler { get; set; }

    public void SignOut() => SignOutHandler?.Invoke();
}
