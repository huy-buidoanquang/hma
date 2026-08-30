using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class LookupWorkspaceViewModel(DispatchOrderService orders, ICurrentUser user, IWorkspaceNavigator nav) : WorkspaceBase
{
    [ObservableProperty] private string? query;
    [ObservableProperty] private DispatchOrder? selected;
    public ObservableCollection<DispatchOrder> Items { get; } = [];

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
                Status = "Nhập số lệnh hoặc biển số.";
                return;
            }
            var q = Query.Trim();
            var byCode = await orders.SearchAsync(q, null, null, null, null, null, null, null, null);
            var byPlate = await orders.SearchAsync(null, null, null, null, q, null, null, null, null);
            foreach (var o in byCode.Concat(byPlate).DistinctBy(x => x.Id).OrderByDescending(x => x.PickupAt))
                Items.Add(o);
            Status = $"{Items.Count} chuyến";
        });
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        nav.OpenDispatch(Selected.Id);
    }
}
