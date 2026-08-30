using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;

namespace Hma.Desktop.Wpf.ViewModels;

public abstract partial class WorkspaceBase : ObservableObject, ILoadableWorkspace
{
    [ObservableProperty] private string? status;
    [ObservableProperty] private string? error;
    [ObservableProperty] private WorkspaceMode mode = WorkspaceMode.Browse;
    [ObservableProperty] private string editorTitle = "";
    [ObservableProperty] private bool isDirty;

    [ObservableProperty] private string? toastMessage;
    [ObservableProperty] private bool toastIsError;
    private int _toastGeneration;

    private ICurrentUser? _currentUser;
    private IUserPrompt? _prompt;
    private string _screenKey = "";

    public bool CanCreate => _currentUser?.Can(_screenKey, PermissionAction.Create) == true;
    public bool CanUpdate => _currentUser?.Can(_screenKey, PermissionAction.Update) == true;
    public bool CanDelete => _currentUser?.Can(_screenKey, PermissionAction.Delete) == true;
    public bool CanView => _currentUser?.Can(_screenKey, PermissionAction.View) == true;
    public bool CanPrint => _currentUser?.Can(_screenKey, PermissionAction.Print) == true;
    public bool CanSave => Mode == WorkspaceMode.Create ? CanCreate : CanUpdate;
    public bool IsBrowsing => Mode == WorkspaceMode.Browse;
    public bool IsEditing => Mode != WorkspaceMode.Browse;
    public bool IsEditMode => Mode == WorkspaceMode.Edit;

    protected void UsePermissions(ICurrentUser user, string screenKey)
    {
        _currentUser = user;
        _screenKey = screenKey;
        NotifyPermissions();
    }

    protected void UsePrompt(IUserPrompt prompt) => _prompt = prompt;

    public virtual Task LoadAsync() => Task.CompletedTask;

    protected void EnterCreate(string title)
    {
        Mode = WorkspaceMode.Create;
        EditorTitle = title;
        IsDirty = true;
        Error = null;
        Status = null;
        NotifyChrome();
    }

    protected void EnterEdit(string title)
    {
        Mode = WorkspaceMode.Edit;
        EditorTitle = title;
        IsDirty = false;
        Error = null;
        Status = null;
        NotifyChrome();
    }

    [RelayCommand]
    private void Cancel() => LeaveEditor();

    protected bool LeaveEditor(bool discardWithoutConfirm = false)
    {
        if (!discardWithoutConfirm && !IsBrowsing && _prompt is not null
            && !_prompt.Confirm("Bỏ thay đổi chưa lưu?", "Xác nhận"))
            return false;
        Mode = WorkspaceMode.Browse;
        EditorTitle = "";
        IsDirty = false;
        NotifyChrome();
        return true;
    }

    protected bool ConfirmDelete() =>
        _prompt?.Confirm("Xóa bản ghi này?", "Xác nhận", UserPromptKind.Destructive) != false;

    protected void ShowToast(string message, bool isError = false)
    {
        ToastMessage = message;
        ToastIsError = isError;
        _ = DismissToastAsync();
    }

    private async Task DismissToastAsync()
    {
        var generation = ++_toastGeneration;
        await Task.Delay(4500);
        if (generation == _toastGeneration)
            ToastMessage = null;
    }

    protected async Task RunAsync(Func<Task> action, string? successToast = null, bool closeEditor = false)
    {
        try
        {
            Error = null;
            await action();
            if (closeEditor)
                LeaveEditor(discardWithoutConfirm: true);
            if (successToast is not null)
                ShowToast(successToast);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            ShowToast(ex.Message, isError: true);
        }
    }

    partial void OnModeChanged(WorkspaceMode value) => NotifyChrome();

    private void NotifyChrome()
    {
        OnPropertyChanged(nameof(IsBrowsing));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(CanSave));
        NotifyPermissions();
    }

    private void NotifyPermissions()
    {
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanUpdate));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanView));
        OnPropertyChanged(nameof(CanPrint));
        OnPropertyChanged(nameof(CanSave));
    }
}
