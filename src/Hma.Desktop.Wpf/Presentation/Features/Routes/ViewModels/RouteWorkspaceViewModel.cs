using Hma.Application.Features.Routes;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Routes.Models;

namespace Hma.Desktop.Wpf.Presentation.Features.Routes.ViewModels;

public partial class RouteWorkspaceViewModel(
    RouteService routes,
    CatalogOptionQueryService catalog,
    ICurrentUser user,
    IUserPrompt prompt, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private string? filterName;
    [ObservableProperty] private RouteEditorModel editor = new();
    [ObservableProperty] private RouteSummary? selected;
    public ObservableCollection<RouteSummary> Items { get; } = [];
    public ObservableCollection<LocationOption> Locations { get; } = [];
    public ObservableCollection<RouteStopDraft> EditorStops { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(
        Editor.Id, Editor.Code, Editor.Name, Editor.Description,
        string.Join(";", EditorStops.Select(stop => stop.LocationId)));

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
        Editor = new RouteEditorModel();
        EditorStops.Clear();
        EditorStops.Add(new RouteStopDraft());
        EditorStops.Add(new RouteStopDraft());
        EnterCreateState("Thêm tuyến", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = RouteEditorModel.From(Selected);
        EditorStops.Clear();
        foreach (var stop in Selected.Stops.OrderBy(s => s.Sequence))
            EditorStops.Add(new RouteStopDraft { LocationId = stop.LocationId });
        if (EditorStops.Count < 2)
        {
            EditorStops.Add(new RouteStopDraft());
            EditorStops.Add(new RouteStopDraft());
        }
        EnterExistingState($"Xem tuyến — {Editor.Code}", $"Sửa tuyến — {Editor.Code}", CaptureEditorState);
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
            var sequence = 0;
            var stops = EditorStops.Select(draft => new SaveRouteStopCommand(sequence++, draft.LocationId)).ToList();
            await routes.SaveAsync(new SaveRouteCommand(
                Editor.Id, Editor.Code, Editor.Name, Editor.Fingerprint, Editor.Description, stops));
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
