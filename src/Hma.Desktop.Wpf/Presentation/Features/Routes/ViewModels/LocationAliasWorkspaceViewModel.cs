using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Routes;
using Hma.Application.Common.Authorization;
using Hma.Desktop.Wpf.Presentation.Common.Formatting;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Hma.Desktop.Wpf.Presentation.Features.Routes.Models;

namespace Hma.Desktop.Wpf.Presentation.Features.Routes.ViewModels;

public partial class LocationAliasWorkspaceViewModel(
    LocationAliasService aliases,
    CatalogOptionQueryService catalog,
    ICurrentUser user,
    IUserPrompt prompt, IUiOperationGate operationGate) : WorkspaceBase(operationGate)
{
    [ObservableProperty] private string? filterText;
    [ObservableProperty] private LocationAliasEditorModel editor = new();
    [ObservableProperty] private LocationAliasSummary? selected;
    public ObservableCollection<LocationAliasSummary> Items { get; } = [];
    public ObservableCollection<LocationOption> Locations { get; } = [];

    public IReadOnlyList<LocationAliasKindOption> KindOptions { get; } =
    [
        new(LocationAliasKind.Both, LocationAliasKindLabels.For(LocationAliasKind.Both)),
        new(LocationAliasKind.Pickup, LocationAliasKindLabels.For(LocationAliasKind.Pickup)),
        new(LocationAliasKind.Delivery, LocationAliasKindLabels.For(LocationAliasKind.Delivery))
    ];

    private EditorState CaptureEditorState() =>
        EditorState.Capture(Editor.Id, Editor.Alias, Editor.LocationId, Editor.Kind);

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
        Editor = new LocationAliasEditorModel { Kind = LocationAliasKind.Both };
        EnterCreateState("Thêm bí danh điểm", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = LocationAliasEditorModel.From(Selected);
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
