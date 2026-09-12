using Hma.Desktop.Wpf.Abstractions;
using Hma.Application.Abstractions.Reporting;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Reporting;
using Hma.Application.Common.Authorization;
using Hma.Application.Features.Settings;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Domain.Entities;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.Presentation.Features.Reporting.ViewModels;

public partial class ReportWorkspaceViewModel(
    ReportQueryService reports,
    DashboardQueryService dashboard,
    CompanyService company,
    IDocumentRenderer printer,
    IDocumentInteractionService documentInteraction,
    ICurrentUser user, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private DateTime day = DateTime.Today;
    [ObservableProperty] private DateTime from = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime to = DateTime.Today;
    public ObservableCollection<CustomerReportRow> CustomerRows { get; } = [];
    public ObservableCollection<VehicleReportRow> VehicleRows { get; } = [];

    public override Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Reports);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void ThisWeek()
    {
        var today = DateTime.Today;
        var offset = ((int)today.DayOfWeek + 6) % 7;
        From = today.AddDays(-offset);
        To = today;
    }

    [RelayCommand]
    private void ThisQuarter()
    {
        var today = DateTime.Today;
        var quarter = (today.Month - 1) / 3;
        From = new DateTime(today.Year, quarter * 3 + 1, 1);
        To = From.AddMonths(3).AddDays(-1);
        if (To > today) To = today;
    }

    [RelayCommand]
    private void ThisYear()
    {
        From = new DateTime(DateTime.Today.Year, 1, 1);
        To = DateTime.Today;
    }

    [RelayCommand]
    private async Task PrintDaily()
    {
        if (!CanPrint) return;
        await RunAsync(async () =>
        {
            var orders = await reports.DailyDispatchAsync(Day);
            var companyInfo = await company.GetAsync();
            await documentInteraction.OpenAsync(printer.PrintDailyDispatch(orders, Day, companyInfo));
            ShowToast($"Đã in {orders.Count} lệnh.");
        });
    }

    [RelayCommand]
    private async Task PrintPeriod()
    {
        if (!CanPrint) return;
        await RunAsync(async () =>
        {
            var orders = await reports.PeriodDispatchAsync(From, To);
            var companyInfo = await company.GetAsync();
            await documentInteraction.OpenAsync(printer.PrintDispatchSummary(
                orders,
                $"BÁO CÁO LỆNH ĐIỀU XE {From:dd/MM/yyyy} – {To:dd/MM/yyyy}",
                companyInfo));
            ShowToast($"Đã in {orders.Count} lệnh.");
        });
    }

    [RelayCommand]
    private async Task ExportPeriodExcel()
    {
        if (!CanPrint) return;
        await RunAsync(async () =>
        {
            var orders = await reports.PeriodDispatchAsync(From, To);
            await documentInteraction.OpenAsync(printer.ExportPeriodSummaryExcel(orders, From, To));
            ShowToast($"Đã xuất Excel {orders.Count} lệnh.");
        });
    }

    [RelayCommand]
    private async Task LoadPeriod()
    {
        CustomerRows.Clear();
        foreach (var r in await dashboard.ByCustomerAsync(From, To)) CustomerRows.Add(r);
        VehicleRows.Clear();
        foreach (var r in await dashboard.ByVehicleAsync(From, To)) VehicleRows.Add(r);
    }
}
