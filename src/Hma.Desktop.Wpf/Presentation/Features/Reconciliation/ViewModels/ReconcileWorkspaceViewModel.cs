using Hma.Application.Features.Reconciliation;
using Hma.Application.Features.Dispatching;
using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.Presentation.Features.Reconciliation.ViewModels;

public partial class ReconcileWorkspaceViewModel(DispatchOrderQueryService queries, DispatchReconciliationService reconciliation, ChangeLogService logs, ICurrentUser user, IWorkspaceNavigator nav, IUiOperationGate operationGate) : WorkspaceBase(operationGate)
{
    [ObservableProperty] private DispatchOrderSummary? selected;
    [ObservableProperty] private string? filterCode;
    [ObservableProperty] private string rejectionReason = "";
    public ObservableCollection<DispatchOrderSummary> Items { get; } = [];
    public ObservableCollection<AuditEntrySummary> History { get; } = [];

    public string Checklist => BuildChecklist(Selected);

    internal static string BuildChecklist(DispatchOrderSummary? selected)
    {
        if (selected is null) return "";
        var hasDoc = selected.HasDeliveryNote;
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
        && Selected.ReconciliationSubmittedByUserId != user.UserId;

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
            var waiting = new List<DispatchOrderSummary>();
            foreach (var status in new[]
                     {
                         ReconciliationStatus.Pending,
                         ReconciliationStatus.Submitted,
                         ReconciliationStatus.Rejected
                     })
            {
                waiting.AddRange(await queries.SearchAsync(
                    FilterCode, null, null, null, null, (int)DispatchStatus.Completed, (int)status, null, null));
            }
            foreach (var o in waiting.OrderByDescending(x => x.PickupAt).ThenByDescending(x => x.Id))
                Items.Add(o);
            Status = $"{Items.Count} chuyến chờ đối soát";
        });
    }

    partial void OnSelectedChanged(DispatchOrderSummary? value)
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
            await OperationGate.RunAsync(async () =>
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
            await reconciliation.SubmitAsync(Selected.Id);
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
            await reconciliation.ApproveAsync(Selected.Id);
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
            await reconciliation.RejectAsync(Selected.Id, RejectionReason);
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
