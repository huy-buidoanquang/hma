namespace Hma.Desktop.Wpf.ViewModels;

public sealed class WorkspaceNavigator : IWorkspaceNavigator
{
    public Action<int>? OpenDispatchHandler { get; set; }

    public void OpenDispatch(int id) => OpenDispatchHandler?.Invoke(id);
}
