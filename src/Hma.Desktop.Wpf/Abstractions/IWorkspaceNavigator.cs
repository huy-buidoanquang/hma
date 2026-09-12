namespace Hma.Desktop.Wpf.Abstractions;

public interface IWorkspaceNavigator
{
    event Action<int>? DispatchRequested;

    void OpenDispatch(int id);
}
