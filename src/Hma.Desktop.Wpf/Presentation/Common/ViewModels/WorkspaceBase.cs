using Hma.Application.Common.Authorization;
using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Hma.Desktop.Wpf.Presentation.Common.ViewModels;

public abstract partial class WorkspaceBase(IUiOperationGate operationGate) : ObservableObject, ILoadableWorkspace
{
    [ObservableProperty] private string? status;
    [ObservableProperty] private string? error;
    [ObservableProperty] private WorkspaceMode mode = WorkspaceMode.Browse;
    [ObservableProperty] private string editorTitle = "";
    [ObservableProperty] private bool isDirty;

    [ObservableProperty] private string? toastMessage;
    [ObservableProperty] private bool toastIsError;
    private int _toastGeneration;
    private Func<EditorState>? _captureEditorState;
    private EditorState? _editorStateBaseline;

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
    protected bool IsEditorStateDirty =>
        _captureEditorState is not null && _captureEditorState() != _editorStateBaseline;

    public virtual bool HasUnsavedChanges =>
        Mode is WorkspaceMode.Create or WorkspaceMode.Edit
        && IsEditorStateDirty;

    protected bool CanEditExisting => string.IsNullOrEmpty(_screenKey) || CanUpdate;
    protected IUiOperationGate OperationGate { get; } = operationGate;

    protected virtual void OnWorkspaceModeChanged()
    {
    }

    protected void UsePermissions(ICurrentUser user, string screenKey)
    {
        _currentUser = user;
        _screenKey = screenKey;
        NotifyPermissions();
    }

    protected void UsePrompt(IUserPrompt prompt) => _prompt = prompt;

    public virtual Task LoadAsync() => Task.CompletedTask;

    protected void EnterCreateState(string title, Func<EditorState> captureState) =>
        BeginEditorState(WorkspaceMode.Create, title, captureState);

    protected void EnterExistingState(string viewTitle, string editTitle, Func<EditorState> captureState) =>
        BeginEditorState(CanEditExisting ? WorkspaceMode.Edit : WorkspaceMode.View,
            CanEditExisting ? editTitle : viewTitle, captureState);

    protected void EnterExistingState(
        string viewTitle,
        string editTitle,
        bool canEdit,
        Func<EditorState> captureState) =>
        BeginEditorState(canEdit ? WorkspaceMode.Edit : WorkspaceMode.View,
            canEdit ? editTitle : viewTitle, captureState);

    protected void RecaptureEditorState()
    {
        if (_captureEditorState is not null)
            _editorStateBaseline = _captureEditorState();
        IsDirty = false;
    }

    private void BeginEditorState(WorkspaceMode mode, string title, Func<EditorState> captureState)
    {
        ArgumentNullException.ThrowIfNull(captureState);
        Mode = mode;
        EditorTitle = title;
        _captureEditorState = captureState;
        _editorStateBaseline = captureState();
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
        _captureEditorState = null;
        _editorStateBaseline = null;
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
            await OperationGate.RunAsync(action);
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
        OnWorkspaceModeChanged();
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
