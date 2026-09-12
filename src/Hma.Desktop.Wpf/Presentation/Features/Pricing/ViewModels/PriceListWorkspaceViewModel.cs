using Hma.Application.Features.Customers;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Common.Authorization;
using Hma.Application.Features.Pricing;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Pricing.Models;

namespace Hma.Desktop.Wpf.Presentation.Features.Pricing.ViewModels;

public partial class PriceListWorkspaceViewModel(
    PriceListService prices,
    CatalogOptionQueryService catalog,
    CustomerService customers,
    ICurrentUser user,
    IUserPrompt prompt, IUiOperationGate operationGate) : WorkspaceBase(operationGate)
{
    [ObservableProperty] private PriceListEditorModel editor = new();
    [ObservableProperty] private PriceListSummary? selected;
    [ObservableProperty] private PriceListItemSummary? selectedItem;
    [ObservableProperty] private int? selectedRouteId;
    [ObservableProperty] private int? selectedDeliveryLocationId;
    [ObservableProperty] private int? selectedVehicleTypeId;
    [ObservableProperty] private decimal unitPrice;
    [ObservableProperty] private decimal surcharge;
    public ObservableCollection<PriceListSummary> Items { get; } = [];
    public ObservableCollection<RouteOption> Routes { get; } = [];
    public ObservableCollection<LocationOption> Locations { get; } = [];
    public ObservableCollection<CustomerSummary> Customers { get; } = [];
    public ObservableCollection<VehicleTypeOption> VehicleTypes { get; } = [];
    public ObservableCollection<PriceListItemSummary> RevisionItems { get; } = [];
    private int? _revisionId;
    private byte[] _versionToken = [];

    private EditorState CaptureEditorState() => EditorState.Capture(
        Editor.Id, Editor.Code, Editor.Name, Editor.Description, Editor.CustomerId,
        Editor.EffectiveFrom, Editor.EffectiveTo, Editor.HasPriceFluctuation, Editor.IsLocked,
        Editor.LockReason,
        string.Join(";", RevisionItems.Select(item =>
            $"{item.Id},{item.RouteId},{item.DeliveryLocationId},{item.VehicleTypeId},{item.UnitPrice},{item.Surcharge}")));
    public override bool IsEditorReadOnly => IsViewMode || Editor.IsLocked;
    public override bool AreFieldsEnabled => !IsEditorReadOnly;
    public bool CanLock => user.IsManager && CanUpdate && Editor.Id != 0 && !Editor.IsLocked;
    public bool CanAddRate => CanUpdate
                              && IsEditing
                              && Editor.Id != 0
                              && !Editor.IsLocked
                              && SelectedVehicleTypeId is not null
                              && (SelectedRouteId is not null || SelectedDeliveryLocationId is not null);
    public bool CanDeleteRate => CanDelete
                                 && IsEditing
                                 && !Editor.IsLocked
                                 && SelectedItem is not null;
    public bool NeedsSavedHeader => IsEditing && Editor.Id == 0;

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.PriceLists);
        UsePrompt(prompt);
        Items.Clear();
        foreach (var p in await prices.ListAsync()) Items.Add(p);
        Routes.Clear();
        foreach (var r in await catalog.RoutesAsync()) Routes.Add(r);
        Locations.Clear();
        foreach (var location in await catalog.LocationsAsync()) Locations.Add(location);
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
        Editor = new PriceListEditorModel { EffectiveFrom = DateTime.Today };
        RevisionItems.Clear();
        _revisionId = null;
        _versionToken = [];
        NotifyActions();
        EnterCreateState("Thêm bảng giá", CaptureEditorState);
    }

    [RelayCommand]
    private async Task ViewItem()
    {
        if (Selected is null) return;
        await OpenAsync(Selected.Id);
        EnterExistingState($"Xem bảng giá — {Editor.Code}", $"Sửa bảng giá — {Editor.Code}",
            CanEditExisting && !Editor.IsLocked, CaptureEditorState);
    }

    private async Task OpenAsync(int id)
    {
        var full = await prices.GetAsync(id);
        if (full is null) return;
        Editor = PriceListEditorModel.From(full.Header);
        _versionToken = full.Header.VersionToken.ToArray();
        _revisionId = full.CurrentRevisionId;
        RevisionItems.Clear();
        foreach (var i in full.Items) RevisionItems.Add(i);
        OnPropertyChanged(nameof(Editor));
        NotifyActions();
        if (!IsBrowsing)
            RecaptureEditorState();
    }

    [RelayCommand]
    private async Task SaveList()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            var saved = await prices.SaveAsync(Editor.ToCommand(_versionToken));
            Editor.Id = saved.Id;
            _revisionId = await prices.EnsureRevisionAsync(saved.Id);
            await LoadAsync();
        }, "Đã lưu bảng giá.", closeEditor: true);
    }

    [RelayCommand]
    private async Task AddRate()
    {
        if (!CanAddRate || SelectedVehicleTypeId is not int vehicleTypeId) return;
        await RunAsync(async () =>
        {
            _revisionId ??= await prices.EnsureRevisionAsync(Editor.Id);
            await prices.AddItemAsync(new AddPriceListItemCommand(
                _revisionId.Value, SelectedRouteId, SelectedDeliveryLocationId, vehicleTypeId, UnitPrice, Surcharge));
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
        if (!CanLock) return;
        await RunAsync(async () =>
        {
            await prices.LockAsync(Editor.Id, Editor.LockReason);
            await OpenAsync(Editor.Id);
        }, "Đã khóa bảng giá.");
    }

    partial void OnSelectedRouteIdChanged(int? value)
    {
        if (value is not null)
            SelectedDeliveryLocationId = null;
        OnPropertyChanged(nameof(CanAddRate));
    }

    partial void OnSelectedDeliveryLocationIdChanged(int? value)
    {
        if (value is not null)
            SelectedRouteId = null;
        OnPropertyChanged(nameof(CanAddRate));
    }

    partial void OnSelectedVehicleTypeIdChanged(int? value) => OnPropertyChanged(nameof(CanAddRate));
    partial void OnSelectedItemChanged(PriceListItemSummary? value) => OnPropertyChanged(nameof(CanDeleteRate));
    partial void OnEditorChanged(PriceListEditorModel value) => NotifyActions();

    protected override void OnWorkspaceModeChanged() => NotifyActions();

    private void NotifyActions()
    {
        OnPropertyChanged(nameof(IsEditorReadOnly));
        OnPropertyChanged(nameof(AreFieldsEnabled));
        OnPropertyChanged(nameof(CanLock));
        OnPropertyChanged(nameof(CanAddRate));
        OnPropertyChanged(nameof(CanDeleteRate));
        OnPropertyChanged(nameof(NeedsSavedHeader));
    }
}
