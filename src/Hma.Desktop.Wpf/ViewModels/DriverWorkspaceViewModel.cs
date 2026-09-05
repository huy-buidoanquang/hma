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

public partial class DriverWorkspaceViewModel(CatalogService catalog, ICurrentUser user, IUserPrompt prompt, IDocumentPrinter printer) : WorkspaceBase
{
    [ObservableProperty] private string? filterCode;
    [ObservableProperty] private string? filterName;
    [ObservableProperty] private Driver? selected;
    [ObservableProperty] private Driver editor = new();
    public ObservableCollection<Driver> Items { get; } = [];
    public ObservableCollection<Partner> Partners { get; } = [];
    public ObservableCollection<DispatchOrder> Trips { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Drivers);
        UsePrompt(prompt);
        await RunAsync(async () =>
        {
            Partners.Clear();
            foreach (var p in await catalog.PartnersAsync()) Partners.Add(p);
            await Search();
        });
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var d in await catalog.DriversAsync(FilterCode, FilterName)) Items.Add(d);
        Status = $"{Items.Count} tài xế";
    }

    [RelayCommand]
    private async Task ResetFilters()
    {
        FilterCode = FilterName = null;
        await Search();
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new Driver();
        Trips.Clear();
        EnterCreate("Thêm tài xế", Editor);
    }

    [RelayCommand]
    private async Task ViewItem()
    {
        if (Selected is null) return;
        Editor = new Driver
        {
            Id = Selected.Id, Code = Selected.Code, Name = Selected.Name, Phone = Selected.Phone,
            BirthDate = Selected.BirthDate, IdentityNumber = Selected.IdentityNumber, PartnerId = Selected.PartnerId
        };
        Trips.Clear();
        foreach (var t in await catalog.TripsByDriverAsync(Editor.Id)) Trips.Add(t);
        EnterExisting($"Xem tài xế — {Editor.Code}", $"Sửa tài xế — {Editor.Code}", Editor);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await catalog.SaveDriverAsync(Editor);
            await Search();
        }, "Đã lưu tài xế.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await catalog.DeleteAsync<Driver>(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (!CanPrint) return;
        var path = Path.Combine(Path.GetTempPath(), "DS-TAI-XE.xlsx");
        printer.ExportDriversExcel(Items.ToList(), path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Status = "Đã xuất Excel tài xế.";
    }
}
