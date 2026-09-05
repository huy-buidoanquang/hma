using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class LocationAliasWorkspaceViewModel(
    LocationAliasService aliases,
    CatalogService catalog,
    ICurrentUser user,
    IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private string? filterText;
    [ObservableProperty] private LocationAlias editor = new();
    [ObservableProperty] private LocationAlias? selected;
    public ObservableCollection<LocationAlias> Items { get; } = [];
    public ObservableCollection<Location> Locations { get; } = [];

    public IReadOnlyList<LocationAliasKindOption> KindOptions { get; } =
    [
        new(LocationAliasKind.Both, LocationAliasKindLabels.For(LocationAliasKind.Both)),
        new(LocationAliasKind.Pickup, LocationAliasKindLabels.For(LocationAliasKind.Pickup)),
        new(LocationAliasKind.Delivery, LocationAliasKindLabels.For(LocationAliasKind.Delivery))
    ];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Settings);
        UsePrompt(prompt);
        DiscardUnsavedEditor();
        Locations.Clear();
        foreach (var location in await catalog.LocationsAsync())
            Locations.Add(location);
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
                || (row.Location?.Code.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (row.Location?.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false))
                Items.Add(row);
        }
        Status = $"{Items.Count} bí danh điểm";
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
        Editor = new LocationAlias { Kind = LocationAliasKind.Both };
        EnterCreate("Thêm bí danh điểm", Editor);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = new LocationAlias
        {
            Id = Selected.Id,
            Alias = Selected.Alias,
            LocationId = Selected.LocationId,
            Kind = Selected.Kind
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
        }, "Đã lưu bí danh điểm.", closeEditor: true);
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
