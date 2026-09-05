using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;

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
    private string? _baseline;
    private object?[]? _fingerprintParts;

    private ICurrentUser? _currentUser;
    private IUserPrompt? _prompt;
    private string _screenKey = "";

    public bool CanCreate => _currentUser?.Can(_screenKey, PermissionAction.Create) == true;
    public bool CanUpdate => _currentUser?.Can(_screenKey, PermissionAction.Update) == true;
    public bool CanDelete => _currentUser?.Can(_screenKey, PermissionAction.Delete) == true;
    public bool CanView => _currentUser?.Can(_screenKey, PermissionAction.View) == true;
    public bool CanPrint => _currentUser?.Can(_screenKey, PermissionAction.Print) == true;
    public bool CanSave => Mode switch
    {
        WorkspaceMode.Create => CanCreate || string.IsNullOrEmpty(_screenKey),
        WorkspaceMode.Edit => CanUpdate || string.IsNullOrEmpty(_screenKey),
        _ => false
    };
    public bool IsBrowsing => Mode == WorkspaceMode.Browse;
    public bool IsEditing => Mode != WorkspaceMode.Browse;
    public bool IsEditMode => Mode == WorkspaceMode.Edit;
    public bool IsViewMode => Mode == WorkspaceMode.View;
    public virtual bool AreFieldsEnabled => Mode is WorkspaceMode.Create or WorkspaceMode.Edit;
    public virtual bool IsEditorReadOnly => Mode == WorkspaceMode.View;
    protected bool IsFingerprintDirty =>
        _fingerprintParts is not null
        && EditorFingerprint.Of(_fingerprintParts) != _baseline;

    public virtual bool HasUnsavedChanges =>
        Mode is WorkspaceMode.Create or WorkspaceMode.Edit
        && IsFingerprintDirty;

    protected bool CanEditExisting => string.IsNullOrEmpty(_screenKey) || CanUpdate;

    protected void UsePermissions(ICurrentUser user, string screenKey)
    {
        _currentUser = user;
        _screenKey = screenKey;
        NotifyPermissions();
    }

    protected void UsePrompt(IUserPrompt prompt) => _prompt = prompt;

    public virtual Task LoadAsync() => Task.CompletedTask;

    protected void EnterCreate(string title, params object?[] snapshot)
    {
        BeginEditor(WorkspaceMode.Create, title, snapshot);
    }

    protected void EnterEdit(string title, params object?[] snapshot)
    {
        BeginEditor(WorkspaceMode.Edit, title, snapshot);
    }

    protected void EnterView(string title, params object?[] snapshot)
    {
        BeginEditor(WorkspaceMode.View, title, snapshot);
    }

    protected void EnterExisting(string viewTitle, string editTitle, params object?[] snapshot) =>
        EnterExisting(viewTitle, editTitle, CanEditExisting, snapshot);

    protected void EnterExisting(string viewTitle, string editTitle, bool canEdit, params object?[] snapshot)
    {
        if (canEdit)
            EnterEdit(editTitle, snapshot);
        else
            EnterView(viewTitle, snapshot);
    }

    protected void RecaptureBaseline(params object?[] snapshot)
    {
        _fingerprintParts = snapshot;
        _baseline = EditorFingerprint.Of(snapshot);
        IsDirty = false;
    }

    private void BeginEditor(WorkspaceMode mode, string title, object?[] snapshot)
    {
        Mode = mode;
        EditorTitle = title;
        _fingerprintParts = snapshot;
        _baseline = EditorFingerprint.Of(snapshot);
        IsDirty = false;
        Error = null;
        Status = null;
        NotifyChrome();
    }

    [RelayCommand]
    private void Cancel() => LeaveEditor();

    public void DiscardUnsavedEditor() => LeaveEditor(discardWithoutConfirm: true);

    protected bool LeaveEditor(bool discardWithoutConfirm = false)
    {
        if (!discardWithoutConfirm && HasUnsavedChanges && _prompt is not null
            && !_prompt.Confirm("Bỏ thay đổi chưa lưu?", "Xác nhận"))
            return false;
        Mode = WorkspaceMode.Browse;
        EditorTitle = "";
        IsDirty = false;
        _baseline = null;
        _fingerprintParts = null;
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
            await SessionDbGate.RunAsync(action);
            if (closeEditor)
                LeaveEditor(discardWithoutConfirm: true);
            if (successToast is not null)
                ShowToast(successToast);
        }
        catch (Exception ex)
        {
            var translated = PersistenceGuard.Translate(ex);
            Error = translated.Message;
            ShowToast(translated.Message, isError: true);
        }
    }

    partial void OnModeChanged(WorkspaceMode value) => NotifyChrome();

    private void NotifyChrome()
    {
        OnPropertyChanged(nameof(IsBrowsing));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(IsViewMode));
        OnPropertyChanged(nameof(AreFieldsEnabled));
        OnPropertyChanged(nameof(IsEditorReadOnly));
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
