using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class ReconcileWorkspaceViewModel(DispatchOrderService orders, ChangeLogService logs, ICurrentUser user, IWorkspaceNavigator nav) : WorkspaceBase
{
    [ObservableProperty] private DispatchOrder? selected;
    [ObservableProperty] private string? filterCode;
    public ObservableCollection<DispatchOrder> Items { get; } = [];
    public ObservableCollection<ChangeLog> History { get; } = [];

    public string Checklist
    {
        get
        {
            if (Selected is null) return "";
            var hasDoc = Selected.Documents.Any(d => d.Kind == DispatchDocumentKind.DeliveryNote);
            return $"Biên bản giao hàng: {(hasDoc ? "Có" : "Không bắt buộc")}\n"
                   + $"Tuyến: {Selected.RouteLabel}\n"
                   + $"Đơn giá: {Selected.UnitPrice:N0}\n"
                   + $"Phụ phí: {Selected.Surcharge:N0}\n"
                   + $"Phát sinh: {Selected.ExtraCost:N0}\n"
                   + $"Tổng: {Selected.TotalAmount:N0}\n"
                   + $"Trạng thái lệnh: {Selected.Status}\n"
                   + $"Đối soát: {Selected.ReconciliationStatus}";
        }
    }

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Reconcile);
        await Search();
    }

    [RelayCommand]
    private async Task Search()
    {
        await RunAsync(async () =>
        {
            Items.Clear();
            foreach (var o in await orders.SearchAsync(FilterCode, null, null, null, null, (int)DispatchStatus.Completed, (int)ReconciliationStatus.Pending, null, null))
                Items.Add(o);
            Status = $"{Items.Count} chuyến chờ đối soát";
        });
    }

    partial void OnSelectedChanged(DispatchOrder? value)
    {
        OnPropertyChanged(nameof(Checklist));
        _ = LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            await SessionDbGate.RunAsync(async () =>
            {
                History.Clear();
                if (Selected is null) return;
                foreach (var h in await logs.ForEntityAsync("DispatchOrder", Selected.Id))
                    History.Add(h);
            });
        }
        catch (Exception ex)
        {
            ShowToast(PersistenceGuard.Translate(ex).Message, isError: true);
        }
    }

    [RelayCommand]
    private async Task Confirm()
    {
        if (!CanUpdate || Selected is null) return;
        await RunAsync(async () =>
        {
            var full = await orders.GetAsync(Selected.Id);
            if (full is null) return;
            await orders.ReconcileAsync(full.Id);
            Status = "Đã đối soát.";
            await Search();
        });
    }

    [RelayCommand]
    private void OpenOrder()
    {
        if (Selected is null) return;
        nav.OpenDispatch(Selected.Id);
    }
}
