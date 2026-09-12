using Hma.Desktop.Wpf.Abstractions;
using Hma.Application.Abstractions.Reporting;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Dispatching;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Catalogs.Models;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.Presentation.Features.Catalogs.ViewModels;

public partial class VehicleWorkspaceViewModel(
    VehicleService vehicleService,
    CatalogOptionQueryService catalog,
    DispatchOrderQueryService dispatchQueries,
    ICurrentUser user,
    IUserPrompt prompt,
    IDocumentRenderer printer,
    IDocumentInteractionService documentInteraction, IUiOperationGate operationGate) : WorkspaceBase(operationGate)
{
    [ObservableProperty] private string? filterPlate;
    [ObservableProperty] private VehicleSummary? selected;
    [ObservableProperty] private VehicleEditorModel editor = new();
    public ObservableCollection<VehicleSummary> Items { get; } = [];
    public ObservableCollection<PartnerOption> Partners { get; } = [];
    public ObservableCollection<VehicleTypeOption> VehicleTypes { get; } = [];
    public ObservableCollection<DispatchOrderSummary> Trips { get; } = [];

    private EditorState CaptureEditorState() =>
        EditorState.Capture(Editor.Id, Editor.PlateNumber, Editor.PartnerId, Editor.VehicleTypeId, Editor.Tonnage);

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Vehicles);
        UsePrompt(prompt);
        await RunAsync(async () =>
        {
            Partners.Clear();
            foreach (var p in await catalog.PartnersAsync()) Partners.Add(p);
            VehicleTypes.Clear();
            foreach (var t in await catalog.VehicleTypesAsync()) VehicleTypes.Add(t);
            await Search();
        });
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var v in await vehicleService.SearchAsync(FilterPlate)) Items.Add(v);
        Status = $"{Items.Count} xe";
    }

    [RelayCommand]
    private async Task ResetFilters()
    {
        FilterPlate = null;
        await Search();
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new VehicleEditorModel();
        Trips.Clear();
        EnterCreateState("Thêm xe", CaptureEditorState);
    }

    [RelayCommand]
    private async Task ViewItem()
    {
        if (Selected is null) return;
        Editor = VehicleEditorModel.From(Selected);
        Trips.Clear();
        foreach (var t in await dispatchQueries.TripsByVehicleAsync(Editor.Id)) Trips.Add(t);
        EnterExistingState($"Xem xe — {Editor.PlateNumber}", $"Sửa xe — {Editor.PlateNumber}", CaptureEditorState);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await vehicleService.SaveAsync(Editor.ToCommand());
            await Search();
        }, "Đã lưu xe.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await vehicleService.DeleteAsync(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        if (!CanPrint) return;
        await documentInteraction.OpenAsync(printer.ExportVehiclesExcel(Items.ToList()));
        Status = "Đã xuất Excel xe.";
    }
}
