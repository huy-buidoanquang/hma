using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Reporting;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Hma.Desktop.Wpf.Presentation.Features.Reporting.ViewModels;

public partial class DashboardWorkspaceViewModel(DashboardQueryService dashboard, ICurrentUser user, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private int year = DateTime.Today.Year;
    [ObservableProperty] private int month = DateTime.Today.Month;
    [ObservableProperty] private DashboardSnapshot? snapshot;
    public ObservableCollection<CustomerReportRow> Customers { get; } = [];
    public ObservableCollection<VehicleReportRow> Vehicles { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Dashboard);
        await Refresh();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await RunAsync(async () =>
        {
            Snapshot = await dashboard.MonthAsync(Year, Month);
            var from = new DateTime(Year, Month, 1);
            var to = from.AddMonths(1).AddTicks(-1);
            Customers.Clear();
            foreach (var r in await dashboard.ByCustomerAsync(from, to)) Customers.Add(r);
            Vehicles.Clear();
            foreach (var r in await dashboard.ByVehicleAsync(from, to)) Vehicles.Add(r);
        });
    }
}
