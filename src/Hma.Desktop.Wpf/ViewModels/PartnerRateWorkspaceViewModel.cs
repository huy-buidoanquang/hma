using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class PartnerRateWorkspaceViewModel(
    PartnerRateService rates,
    CatalogService catalog,
    ICurrentUser current,
    IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private PartnerRate editor = new();
    [ObservableProperty] private PartnerRate? selected;

    public ObservableCollection<PartnerRate> Items { get; } = [];
    public ObservableCollection<Partner> Partners { get; } = [];
    public ObservableCollection<Route> Routes { get; } = [];
    public ObservableCollection<VehicleType> VehicleTypes { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(current, ScreenKeys.PartnerRates);
        UsePrompt(prompt);
        Partners.Clear();
        foreach (var partner in await catalog.PartnersAsync())
            if (partner.Code != "UNASSIGNED") Partners.Add(partner);
        Routes.Clear();
        foreach (var route in await catalog.RoutesAsync()) Routes.Add(route);
        VehicleTypes.Clear();
        foreach (var type in await catalog.VehicleTypesAsync()) VehicleTypes.Add(type);
        await Search();
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var rate in await rates.ListAsync()) Items.Add(rate);
        Status = $"{Items.Count} dòng giá mua";
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Editor = new PartnerRate { EffectiveFrom = DateTime.Today };
        Selected = null;
        EnterCreate("Thêm giá mua đối tác", Editor);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = new PartnerRate
        {
            Id = Selected.Id,
            PartnerId = Selected.PartnerId,
            RouteId = Selected.RouteId,
            VehicleTypeId = Selected.VehicleTypeId,
            EffectiveFrom = Selected.EffectiveFrom,
            EffectiveTo = Selected.EffectiveTo,
            UnitPrice = Selected.UnitPrice,
            Surcharge = Selected.Surcharge,
            CreatedAt = Selected.CreatedAt,
            CreatedByUserId = Selected.CreatedByUserId,
            RowVersion = Selected.RowVersion.ToArray()
        };
        EnterExisting("Xem giá mua đối tác", "Sửa giá mua đối tác", Editor);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await rates.SaveAsync(Editor);
            await Search();
        }, "Đã lưu giá mua đối tác.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await rates.DeleteAsync(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa giá mua đối tác.");
    }
}
