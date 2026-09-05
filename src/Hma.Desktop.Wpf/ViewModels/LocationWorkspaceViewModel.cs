using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class LocationWorkspaceViewModel(LocationService locations, CatalogService catalog, ICurrentUser user, IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private string? filterName;
    [ObservableProperty] private Location editor = new();
    [ObservableProperty] private Location? selected;
    public ObservableCollection<Location> Items { get; } = [];
    public ObservableCollection<City> Cities { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Locations);
        UsePrompt(prompt);
        Cities.Clear();
        foreach (var city in await catalog.CitiesAsync())
            Cities.Add(city);
        await Search();
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var row in await locations.ListAsync())
            if (string.IsNullOrWhiteSpace(FilterName)
                || row.Code.Contains(FilterName, StringComparison.OrdinalIgnoreCase)
                || row.Name.Contains(FilterName, StringComparison.OrdinalIgnoreCase))
                Items.Add(row);
        Status = $"{Items.Count} điểm";
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
        Editor = new Location();
        EnterCreate("Thêm điểm", Editor);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = new Location
        {
            Id = Selected.Id,
            Code = Selected.Code,
            Name = Selected.Name,
            Description = Selected.Description,
            CityId = Selected.CityId
        };
        EnterExisting($"Xem điểm — {Editor.Code}", $"Sửa điểm — {Editor.Code}", Editor);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await locations.SaveAsync(Editor);
            await Search();
        }, "Đã lưu điểm.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await locations.DeleteAsync(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }
}
