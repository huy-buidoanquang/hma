using Hma.Application.Features.Routes;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Routes.Models;

namespace Hma.Desktop.Wpf.Presentation.Features.Routes.ViewModels;

public partial class LocationWorkspaceViewModel(LocationService locations, CatalogOptionQueryService catalog, ICurrentUser user, IUserPrompt prompt, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private string? filterName;
    [ObservableProperty] private LocationEditorModel editor = new();
    [ObservableProperty] private LocationSummary? selected;
    public ObservableCollection<LocationSummary> Items { get; } = [];
    public ObservableCollection<CatalogOption> Cities { get; } = [];

    private EditorState CaptureEditorState() =>
        EditorState.Capture(Editor.Id, Editor.Code, Editor.Name, Editor.Description, Editor.CityId);

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
        Editor = new LocationEditorModel();
        EnterCreateState("Thêm điểm", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = LocationEditorModel.From(Selected);
        EnterExistingState($"Xem điểm — {Editor.Code}", $"Sửa điểm — {Editor.Code}", CaptureEditorState);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await locations.SaveAsync(Editor.ToCommand());
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
