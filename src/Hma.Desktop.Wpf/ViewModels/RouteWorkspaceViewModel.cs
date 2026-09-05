using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class RouteWorkspaceViewModel(
    RouteService routes,
    CatalogService catalog,
    ICurrentUser user,
    IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private string? filterName;
    [ObservableProperty] private Route editor = new();
    [ObservableProperty] private Route? selected;
    public ObservableCollection<Route> Items { get; } = [];
    public ObservableCollection<Location> Locations { get; } = [];
    public ObservableCollection<RouteStopDraft> EditorStops { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Routes);
        UsePrompt(prompt);
        Locations.Clear();
        foreach (var location in await catalog.LocationsAsync())
            Locations.Add(location);
        await Search();
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var row in await routes.ListAsync())
            if (string.IsNullOrWhiteSpace(FilterName)
                || row.Code.Contains(FilterName, StringComparison.OrdinalIgnoreCase)
                || row.Name.Contains(FilterName, StringComparison.OrdinalIgnoreCase))
                Items.Add(row);
        Status = $"{Items.Count} tuyến";
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
        Editor = new Route();
        EditorStops.Clear();
        EditorStops.Add(new RouteStopDraft());
        EditorStops.Add(new RouteStopDraft());
        EnterCreate("Thêm tuyến", Editor, EditorStops);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = new Route
        {
            Id = Selected.Id,
            Code = Selected.Code,
            Name = Selected.Name,
            Description = Selected.Description,
            Fingerprint = Selected.Fingerprint
        };
        EditorStops.Clear();
        foreach (var stop in Selected.Stops.OrderBy(s => s.Sequence))
            EditorStops.Add(new RouteStopDraft { LocationId = stop.LocationId });
        if (EditorStops.Count < 2)
        {
            EditorStops.Add(new RouteStopDraft());
            EditorStops.Add(new RouteStopDraft());
        }
        EnterExisting($"Xem tuyến — {Editor.Code}", $"Sửa tuyến — {Editor.Code}", Editor, EditorStops);
    }

    [RelayCommand]
    private void AddStop() => EditorStops.Add(new RouteStopDraft());

    [RelayCommand]
    private void RemoveStop(RouteStopDraft? stop)
    {
        if (stop is null || EditorStops.Count <= 2) return;
        EditorStops.Remove(stop);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            Editor.Stops.Clear();
            var sequence = 0;
            foreach (var draft in EditorStops)
            {
                Editor.Stops.Add(new RouteStop
                {
                    Sequence = sequence++,
                    LocationId = draft.LocationId
                });
            }
            await routes.SaveAsync(Editor);
            await Search();
        }, "Đã lưu tuyến.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await routes.DeleteAsync(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }
}
