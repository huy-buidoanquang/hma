using Hma.Desktop.Wpf.Abstractions;

namespace Hma.Desktop.Wpf.Infrastructure.Navigation;

public sealed class WorkspaceNavigator : IWorkspaceNavigator
{
    public event Action<int>? DispatchRequested;

    public void OpenDispatch(int id) => DispatchRequested?.Invoke(id);
}
