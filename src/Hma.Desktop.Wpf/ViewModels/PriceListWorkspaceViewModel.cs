using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class PriceListWorkspaceViewModel(
    PriceListService prices,
    CatalogService catalog,
    CustomerService customers,
    ICurrentUser user,
    IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private PriceList editor = new();
    [ObservableProperty] private PriceList? selected;
    [ObservableProperty] private PriceListRevision? revision;
    [ObservableProperty] private PriceListItem? selectedItem;
    [ObservableProperty] private int? selectedPickupCityId;
    [ObservableProperty] private int? selectedDeliveryCityId;
    [ObservableProperty] private int? selectedVehicleTypeId;
    [ObservableProperty] private decimal unitPrice;
    [ObservableProperty] private decimal surcharge;
    public ObservableCollection<PriceList> Items { get; } = [];
    public ObservableCollection<City> Cities { get; } = [];
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<VehicleType> VehicleTypes { get; } = [];
    public ObservableCollection<PriceListItem> RevisionItems { get; } = [];
    public bool IsEditorReadOnly => Editor.IsLocked;
    public bool AreFieldsEnabled => !Editor.IsLocked;

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.PriceLists);
        UsePrompt(prompt);
        Items.Clear();
        foreach (var p in await prices.ListAsync()) Items.Add(p);
        Cities.Clear();
        foreach (var c in await catalog.CitiesAsync()) Cities.Add(c);
        Customers.Clear();
        foreach (var c in await customers.SearchAsync(null, null, null, null))
            if (!c.IsWalkIn) Customers.Add(c);
        VehicleTypes.Clear();
        foreach (var t in await catalog.VehicleTypesAsync()) VehicleTypes.Add(t);
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new PriceList { EffectiveFrom = DateTime.Today };
        RevisionItems.Clear();
        Revision = null;
        OnPropertyChanged(nameof(IsEditorReadOnly));
        OnPropertyChanged(nameof(AreFieldsEnabled));
        EnterCreate("Thêm bảng giá");
    }

    [RelayCommand]
    private async Task ViewItem()
    {
        if (Selected is null) return;
        await OpenAsync(Selected.Id);
        EnterEdit($"Sửa bảng giá — {Editor.Code}");
    }

    private async Task OpenAsync(int id)
    {
        var full = await prices.GetAsync(id);
        if (full is null) return;
        Editor = full;
        Revision = full.Revisions.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
        RevisionItems.Clear();
        if (Revision is not null)
            foreach (var i in Revision.Items) RevisionItems.Add(i);
        OnPropertyChanged(nameof(Editor));
        OnPropertyChanged(nameof(IsEditorReadOnly));
        OnPropertyChanged(nameof(AreFieldsEnabled));
    }

    [RelayCommand]
    private async Task SaveList()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await prices.SaveAsync(Editor);
            if (Editor.Revisions.Count == 0)
            {
                var rev = new PriceListRevision { PriceListId = Editor.Id, CreatedAt = DateTime.Today };
                await prices.SaveRevisionAsync(rev);
            }
            await LoadAsync();
        }, "Đã lưu bảng giá.", closeEditor: true);
    }

    [RelayCommand]
    private async Task AddRate()
    {
        if (!CanUpdate || Editor.IsLocked) { ShowToast(Editor.IsLocked ? "Bảng giá đã khóa." : "Không sửa được bảng giá.", isError: true); return; }
        if (Editor.Id == 0 || SelectedDeliveryCityId is null || SelectedVehicleTypeId is null) return;
        await RunAsync(async () =>
        {
            if (Revision is null || Revision.Id == 0)
            {
                Revision = new PriceListRevision { PriceListId = Editor.Id, CreatedAt = DateTime.Today };
                await prices.SaveRevisionAsync(Revision);
            }
            await prices.AddItemAsync(new PriceListItem
            {
                PriceListRevisionId = Revision.Id,
                PickupCityId = SelectedPickupCityId,
                DeliveryCityId = SelectedDeliveryCityId.Value,
                VehicleTypeId = SelectedVehicleTypeId.Value,
                UnitPrice = UnitPrice,
                Surcharge = Surcharge
            });
            await OpenAsync(Editor.Id);
        }, "Đã thêm dòng giá.");
    }

    [RelayCommand]
    private async Task DeleteRate()
    {
        if (!CanDelete || Editor.IsLocked || SelectedItem is null) return;
        await RunAsync(async () =>
        {
            await prices.DeleteItemAsync(SelectedItem.Id);
            await OpenAsync(Editor.Id);
        }, "Đã xóa dòng giá.");
    }

    [RelayCommand]
    private async Task LockList()
    {
        if (!CanUpdate || Editor.Id == 0) return;
        await RunAsync(async () =>
        {
            Editor.IsLocked = true;
            Editor.LockedAt = DateTime.Now;
            await prices.SaveAsync(Editor);
            await OpenAsync(Editor.Id);
        }, "Đã khóa bảng giá.");
    }
}
