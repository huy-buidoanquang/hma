using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Desktop.Wpf.Abstractions;

namespace Hma.Desktop.Wpf.Presentation.Shell.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDesktopUserSession _userSession;
    private readonly IUserPrompt _prompt;
    private readonly ISessionHost _session;

    [ObservableProperty] private object? current;
    [ObservableProperty] private WorkspaceEntry? selected;
    [ObservableProperty] private string userLabel = "";

    public System.Collections.ObjectModel.ObservableCollection<WorkspaceEntry> Items { get; } = [];

    public MainViewModel(
        ICurrentUser currentUser,
        IDesktopUserSession userSession,
        IUserPrompt prompt,
        ISessionHost session,
        IWorkspaceNavigator navigator,
        IWorkspaceRegistry registry)
    {
        _userSession = userSession;
        _prompt = prompt;
        _session = session;
        UserLabel = currentUser.DisplayName ?? currentUser.UserName ?? "";

        foreach (var entry in registry.Entries)
            Items.Add(entry);

        navigator.DispatchRequested += OpenDispatch;
        if (Items.Count > 0)
            Selected = Items[0];
    }

    [RelayCommand]
    private void Logout()
    {
        var message = Current is WorkspaceBase { HasUnsavedChanges: true }
            ? "Bỏ thay đổi chưa lưu và đăng xuất?"
            : "Đăng xuất khỏi phiên này?";
        if (!_prompt.Confirm(message, "Đăng xuất"))
            return;

        _userSession.SignOut();
        _session.SignOut();
    }

    partial void OnSelectedChanged(WorkspaceEntry? value)
    {
        if (value is null)
            return;

        Current = value.Workspace;
        if (Current is ILoadableWorkspace loadable)
            _ = LoadWorkspaceAsync(loadable);
    }

    private void OpenDispatch(int id)
    {
        var entry = Items.FirstOrDefault(item => item.Key == ScreenKeys.DispatchOrders);
        if (entry?.Workspace is not DispatchHubWorkspaceViewModel dispatchHub)
            return;

        Selected = entry;
        _ = dispatchHub.OpenOrderAsync(id);
    }

    private static async Task LoadWorkspaceAsync(ILoadableWorkspace loadable)
    {
        try
        {
            await loadable.LoadAsync();
        }
        catch (Exception)
        {
            // WorkspaceBase.RunAsync already toasts; this swallows unobserved fire-and-forget faults.
        }
    }
}
