using Hma.Application.Features.Dispatching;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Hma.Desktop.Wpf.Presentation.Features.Dispatching.ViewModels;

public partial class LookupWorkspaceViewModel(DispatchOrderQueryService queries, ICurrentUser user, IWorkspaceNavigator nav, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private string? query;
    [ObservableProperty] private DispatchOrderSummary? selected;
    public ObservableCollection<DispatchOrderSummary> Items { get; } = [];

    public override Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Lookup);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Search()
    {
        await RunAsync(async () =>
        {
            Items.Clear();
            if (string.IsNullOrWhiteSpace(Query))
            {
                ShowToast("Nhập số lệnh hoặc biển số.", isError: true);
                return;
            }
            var q = Query.Trim();
            var byCode = await queries.SearchAsync(q, null, null, null, null, null, null, null, null);
            var byPlate = await queries.SearchAsync(null, null, null, null, q, null, null, null, null);
            foreach (var o in byCode.Concat(byPlate).DistinctBy(x => x.Id).OrderByDescending(x => x.PickupAt))
                Items.Add(o);
        });
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        nav.OpenDispatch(Selected.Id);
    }
}
