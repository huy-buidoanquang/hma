using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class DispatchGridEditWorkspaceViewModel(
    DispatchOrderService orders,
    CatalogService catalog,
    CustomerService customers,
    ICurrentUser current,
    IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private int? customerId;
    [ObservableProperty] private int year = DateTime.Today.Year;
    [ObservableProperty] private int month = DateTime.Today.Month;
    [ObservableProperty] private DispatchGridRow? selected;
    [ObservableProperty] private bool? allSelected;

    public override bool HasUnsavedChanges => _dirty;

    private bool _suppressAllSelected;
    private bool _dirty;

    public ObservableCollection<DispatchGridRow> Items { get; } = [];
    public ObservableCollection<Customer> CustomerOptions { get; } = [];
    public ObservableCollection<Route> Routes { get; } = [];
    public ObservableCollection<Vehicle> Vehicles { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(current, ScreenKeys.DispatchGridEdit);
        UsePrompt(prompt);
        await RunAsync(async () =>
        {
            CustomerOptions.Clear();
            foreach (var c in await customers.SearchAsync(null, null, null, null))
                if (!c.IsWalkIn) CustomerOptions.Add(c);
            Routes.Clear();
            foreach (var r in await catalog.RoutesAsync()) Routes.Add(r);
            Vehicles.Clear();
            foreach (var v in await catalog.VehiclesAsync()) Vehicles.Add(v);
        });
    }

    [RelayCommand]
    private async Task Search()
    {
        if (CustomerId is null)
        {
            ShowToast("Chọn khách hàng.", isError: true);
            return;
        }
        await RunAsync(SearchCore);
    }

    private async Task SearchCore()
    {
        ClearRows();
        var list = await orders.SearchAsync(
            null, null, null, CustomerId, null, null, null, null, null,
            billingYear: Year, billingMonth: Month);
        foreach (var o in list)
        {
            var row = new DispatchGridRow
            {
                Id = o.Id,
                Code = o.Code,
                PickupAt = o.PickupAt,
                CustomerLabel = o.CustomerCodeName,
                Status = o.Status,
                ReconciliationStatus = o.ReconciliationStatus,
                RouteId = o.RouteId,
                VehicleId = o.VehicleId,
                UnitPrice = o.UnitPrice,
                ExtraCost = o.ExtraCost,
                Notes = o.Notes,
                BillingYear = o.BillingYear == 0 ? o.PickupAt.Year : o.BillingYear,
                BillingMonth = o.BillingMonth == 0 ? o.PickupAt.Month : o.BillingMonth
            };
            row.PropertyChanged += OnRowPropertyChanged;
            Items.Add(row);
        }
        RefreshAllSelected();
        _dirty = false;
        Status = $"{Items.Count} lệnh kỳ {Month:00}/{Year}";
    }

    [RelayCommand]
    private async Task SaveGrid()
    {
        if (!CanUpdate) return;
        var rows = Items.Where(r => r.CanEditRow).ToList();
        if (rows.Count == 0)
        {
            ShowToast("Không có lệnh chưa chốt để lưu.", isError: true);
            return;
        }
        await RunAsync(async () =>
        {
            foreach (var row in rows)
            {
                await orders.SaveGridRowAsync(
                    row.Id, row.UnitPrice, row.ExtraCost, row.Notes,
                    row.BillingYear, row.BillingMonth,
                    row.RouteId, row.VehicleId);
            }
            await SearchCore();
        }, "Đã lưu lưới lệnh.");
    }

    [RelayCommand]
    private async Task ShiftNextMonth()
    {
        if (!CanUpdate) return;
        var ids = Items.Where(r => r.IsSelected && r.CanEditRow).Select(r => r.Id).ToList();
        if (ids.Count == 0)
        {
            ShowToast("Chọn lệnh chưa chốt để chuyển kỳ kế toán.", isError: true);
            return;
        }
        await RunAsync(async () =>
        {
            await orders.ShiftBillingPeriodAsync(ids);
            await SearchCore();
        }, "Đã chuyển sang tháng sau (ngày chạy giữ nguyên).");
    }

    partial void OnAllSelectedChanged(bool? value)
    {
        if (_suppressAllSelected) return;
        var select = value == true;
        foreach (var row in Items.Where(r => r.CanEditRow))
            row.IsSelected = select;
        if (value is null)
            RefreshAllSelected();
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DispatchGridRow.IsSelected))
            RefreshAllSelected();
        else if (e.PropertyName is nameof(DispatchGridRow.RouteId) or nameof(DispatchGridRow.VehicleId)
                 or nameof(DispatchGridRow.UnitPrice) or nameof(DispatchGridRow.ExtraCost)
                 or nameof(DispatchGridRow.Notes) or nameof(DispatchGridRow.BillingYear)
                 or nameof(DispatchGridRow.BillingMonth))
            _dirty = true;
    }

    private void RefreshAllSelected()
    {
        _suppressAllSelected = true;
        var editable = Items.Where(r => r.CanEditRow).ToList();
        if (editable.Count == 0 || editable.All(r => !r.IsSelected))
            AllSelected = false;
        else if (editable.All(r => r.IsSelected))
            AllSelected = true;
        else
            AllSelected = null;
        _suppressAllSelected = false;
    }

    public void DiscardPendingEdits()
    {
        _dirty = false;
        ClearRows();
        Status = null;
    }

    private void ClearRows()
    {
        foreach (var row in Items)
            row.PropertyChanged -= OnRowPropertyChanged;
        Items.Clear();
    }
}
