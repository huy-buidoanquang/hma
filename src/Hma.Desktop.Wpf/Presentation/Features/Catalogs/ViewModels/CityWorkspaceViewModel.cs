using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Catalogs.Models;

namespace Hma.Desktop.Wpf.Presentation.Features.Catalogs.ViewModels;

public partial class CityWorkspaceViewModel(CityService cities, ICurrentUser user, IUserPrompt prompt, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private string? filterName;
    [ObservableProperty] private CatalogItemEditorModel editor = new();
    [ObservableProperty] private CatalogItemSummary? selected;
    public ObservableCollection<CatalogItemSummary> Items { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(Editor.Id, Editor.Code, Editor.Name);

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Cities);
        UsePrompt(prompt);
        await Search();
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var c in await cities.ListAsync())
            if (string.IsNullOrWhiteSpace(FilterName) || c.Code.Contains(FilterName, StringComparison.OrdinalIgnoreCase)
                || c.Name.Contains(FilterName, StringComparison.OrdinalIgnoreCase))
                Items.Add(c);
    }

    [RelayCommand]
    private async Task ResetFilters()
    {
        FilterName = null;
        await Search();
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new CatalogItemEditorModel();
        EnterCreateState("Thêm thành phố", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = CatalogItemEditorModel.From(Selected);
        EnterExistingState($"Xem thành phố — {Editor.Code}", $"Sửa thành phố — {Editor.Code}", CaptureEditorState);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await cities.SaveAsync(Editor.ToCommand());
            await Search();
        }, "Đã lưu thành phố.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await cities.DeleteAsync(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }
}
