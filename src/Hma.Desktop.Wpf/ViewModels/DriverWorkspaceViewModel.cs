using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class DriverWorkspaceViewModel(CatalogService catalog, ICurrentUser user, IUserPrompt prompt) : WorkspaceBase
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
        EnterCreate("Thêm tài xế");
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
        EnterEdit($"Sửa tài xế — {Editor.Code}");
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
}
