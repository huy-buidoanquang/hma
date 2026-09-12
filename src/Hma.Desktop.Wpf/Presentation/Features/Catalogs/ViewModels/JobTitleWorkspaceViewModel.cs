using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Catalogs.Models;

namespace Hma.Desktop.Wpf.Presentation.Features.Catalogs.ViewModels;

public partial class JobTitleWorkspaceViewModel(JobTitleService jobTitles, ICurrentUser user, IUserPrompt prompt, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private CatalogItemSummary? selected;
    [ObservableProperty] private CatalogItemEditorModel editor = new();
    public ObservableCollection<CatalogItemSummary> Items { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(Editor.Id, Editor.Code, Editor.Name);

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.JobTitles);
        UsePrompt(prompt);
        Items.Clear();
        foreach (var j in await jobTitles.ListAsync()) Items.Add(j);
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new CatalogItemEditorModel();
        EnterCreateState("Thêm chức vụ", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = CatalogItemEditorModel.From(Selected);
        EnterExistingState($"Xem chức vụ — {Editor.Code}", $"Sửa chức vụ — {Editor.Code}", CaptureEditorState);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await jobTitles.SaveAsync(Editor.ToCommand());
            await LoadAsync();
        }, "Đã lưu chức vụ.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await jobTitles.DeleteAsync(Editor.Id);
            await LoadAsync();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }
}
