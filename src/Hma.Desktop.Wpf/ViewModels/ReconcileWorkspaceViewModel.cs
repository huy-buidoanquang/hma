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
    [ObservableProperty] private string rejectionReason = "";
    public ObservableCollection<DispatchOrder> Items { get; } = [];
    public ObservableCollection<ChangeLog> History { get; } = [];

    public string Checklist => BuildChecklist(Selected);

    internal static string BuildChecklist(DispatchOrder? selected)
    {
        if (selected is null) return "";
        var hasDoc = selected.Documents.Any(d => d.Kind == DispatchDocumentKind.DeliveryNote);
        return $"Biên bản giao hàng: {(hasDoc ? "Có" : "Thiếu — bắt buộc")}\n"
               + $"Tuyến: {selected.RouteLabel}\n"
               + $"Đơn giá: {selected.UnitPrice:N0}\n"
               + $"Phụ phí: {selected.Surcharge:N0}\n"
               + $"Phát sinh: {selected.BillableExtraCost:N0}\n"
               + $"Tổng: {selected.TotalAmount:N0}\n"
               + $"Trạng thái lệnh: {selected.Status}\n"
               + $"Đối soát: {selected.ReconciliationStatus}";
    }

    public bool CanSubmit =>
        CanUpdate && Selected is not null
        && Selected.Status == DispatchStatus.Completed
        && Selected.ReconciliationStatus is ReconciliationStatus.Pending or ReconciliationStatus.Rejected
        && Selected.HasDeliveryNote;

    public bool CanApprove =>
        CanUpdate && Selected?.ReconciliationStatus == ReconciliationStatus.Submitted
        && Selected.ReconciliationSubmittedByUserId != user.User?.Id;

    public bool CanReject => CanApprove;

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
            var waiting = new List<DispatchOrder>();
            foreach (var status in new[]
                     {
                         ReconciliationStatus.Pending,
                         ReconciliationStatus.Submitted,
                         ReconciliationStatus.Rejected
                     })
            {
                waiting.AddRange(await orders.SearchAsync(
                    FilterCode, null, null, null, null, (int)DispatchStatus.Completed, (int)status, null, null));
            }
            foreach (var o in waiting.OrderByDescending(x => x.PickupAt).ThenByDescending(x => x.Id))
                Items.Add(o);
            Status = $"{Items.Count} chuyến chờ đối soát";
        });
    }

    partial void OnSelectedChanged(DispatchOrder? value)
    {
        OnPropertyChanged(nameof(Checklist));
        OnPropertyChanged(nameof(CanSubmit));
        OnPropertyChanged(nameof(CanApprove));
        OnPropertyChanged(nameof(CanReject));
        RejectionReason = value?.ReconciliationRejectionReason ?? "";
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
    private async Task Submit()
    {
        if (!CanSubmit || Selected is null) return;
        await RunAsync(async () =>
        {
            await orders.SubmitReconciliationAsync(Selected.Id);
            Status = "Đã gửi duyệt đối soát.";
            await Search();
        });
    }

    [RelayCommand]
    private async Task Approve()
    {
        if (!CanApprove || Selected is null) return;
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
    private async Task Reject()
    {
        if (!CanReject || Selected is null) return;
        await RunAsync(async () =>
        {
            await orders.RejectReconciliationAsync(Selected.Id, RejectionReason);
            Status = "Đã từ chối đối soát.";
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
