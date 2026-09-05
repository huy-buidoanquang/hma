using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class RouteAliasWorkspaceViewModel(
    RouteAliasService aliases,
    CatalogService catalog,
    ICurrentUser user,
    IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private string? filterText;
    [ObservableProperty] private RouteAlias editor = new();
    [ObservableProperty] private RouteAlias? selected;
    public ObservableCollection<RouteAlias> Items { get; } = [];
    public ObservableCollection<Route> Routes { get; } = [];

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
        Status = $"{Items.Count} bí danh tuyến";
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
        Editor = new RouteAlias();
        EnterCreate("Thêm bí danh tuyến", Editor);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = new RouteAlias
        {
            Id = Selected.Id,
            Alias = Selected.Alias,
            RouteId = Selected.RouteId
        };
        EnterExisting($"Xem bí danh — {Editor.Alias}", $"Sửa bí danh — {Editor.Alias}", Editor);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await aliases.SaveAsync(Editor);
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
