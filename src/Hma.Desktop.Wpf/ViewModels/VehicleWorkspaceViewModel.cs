using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class VehicleWorkspaceViewModel(CatalogService catalog, ICurrentUser user, IUserPrompt prompt, IDocumentPrinter printer) : WorkspaceBase
{
    [ObservableProperty] private string? filterPlate;
    [ObservableProperty] private Vehicle? selected;
    [ObservableProperty] private Vehicle editor = new();
    public ObservableCollection<Vehicle> Items { get; } = [];
    public ObservableCollection<Partner> Partners { get; } = [];
    public ObservableCollection<VehicleType> VehicleTypes { get; } = [];
    public ObservableCollection<DispatchOrder> Trips { get; } = [];

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
        foreach (var v in await catalog.VehiclesAsync(FilterPlate)) Items.Add(v);
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
        Editor = new Vehicle();
        Trips.Clear();
        EnterCreate("Thêm xe", Editor);
    }

    [RelayCommand]
    private async Task ViewItem()
    {
        if (Selected is null) return;
        Editor = new Vehicle
        {
            Id = Selected.Id, PlateNumber = Selected.PlateNumber, PartnerId = Selected.PartnerId,
            VehicleTypeId = Selected.VehicleTypeId, Tonnage = Selected.Tonnage
        };
        Trips.Clear();
        foreach (var t in await catalog.TripsByVehicleAsync(Editor.Id)) Trips.Add(t);
        EnterExisting($"Xem xe — {Editor.PlateNumber}", $"Sửa xe — {Editor.PlateNumber}", Editor);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await catalog.SaveVehicleAsync(Editor);
            await Search();
        }, "Đã lưu xe.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await catalog.DeleteAsync<Vehicle>(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (!CanPrint) return;
        var path = Path.Combine(Path.GetTempPath(), "DS-XE.xlsx");
        printer.ExportVehiclesExcel(Items.ToList(), path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Status = "Đã xuất Excel xe.";
    }
}
