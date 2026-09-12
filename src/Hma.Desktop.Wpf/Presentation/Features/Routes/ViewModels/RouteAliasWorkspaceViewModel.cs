using Hma.Application.Features.Routes;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Routes.Models;

namespace Hma.Desktop.Wpf.Presentation.Features.Routes.ViewModels;

public partial class RouteAliasWorkspaceViewModel(
    RouteAliasService aliases,
    CatalogOptionQueryService catalog,
    ICurrentUser user,
    IUserPrompt prompt, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private string? filterText;
    [ObservableProperty] private RouteAliasEditorModel editor = new();
    [ObservableProperty] private RouteAliasSummary? selected;
    public ObservableCollection<RouteAliasSummary> Items { get; } = [];
    public ObservableCollection<RouteOption> Routes { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(Editor.Id, Editor.Alias, Editor.RouteId);

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Settings);
        UsePrompt(prompt);
        DiscardUnsavedEditor();
        Routes.Clear();
        foreach (var route in await catalog.RoutesAsync())
            Routes.Add(route);
        await Search();
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var row in await aliases.ListAsync())
        {
            if (string.IsNullOrWhiteSpace(FilterText)
                || row.Alias.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
                || (row.Route?.Code.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (row.Route?.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false))
                Items.Add(row);
        }
    }

    [RelayCommand]
    private async Task ResetFilters()
    {
        FilterText = null;
        await Search();
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new RouteAliasEditorModel();
        EnterCreateState("Thêm bí danh tuyến", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = RouteAliasEditorModel.From(Selected);
        EnterExistingState($"Xem bí danh — {Editor.Alias}", $"Sửa bí danh — {Editor.Alias}", CaptureEditorState);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await aliases.SaveAsync(Editor.ToCommand());
            await Search();
        }, "Đã lưu bí danh tuyến.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await aliases.DeleteAsync(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }
}
