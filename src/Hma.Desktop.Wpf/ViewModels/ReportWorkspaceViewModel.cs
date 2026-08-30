using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class ReportWorkspaceViewModel(ReportQueryService reports, DashboardQueryService dashboard, CompanyService company, IDocumentPrinter printer, ICurrentUser user) : WorkspaceBase
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
        var orders = await reports.DailyDispatchAsync(Day);
        var companyInfo = await company.GetAsync();
        var path = printer.PrintDailyDispatch(orders, Day, companyInfo);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Status = $"Đã in {orders.Count} lệnh.";
    }

    [RelayCommand]
    private async Task PrintPeriod()
    {
        if (!CanPrint) return;
        var orders = await reports.PeriodDispatchAsync(From, To);
        var companyInfo = await company.GetAsync();
        var path = printer.PrintDispatchSummary(orders, $"BÁO CÁO LỆNH ĐIỀU XE {From:dd/MM/yyyy} – {To:dd/MM/yyyy}", companyInfo);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Status = $"Đã in {orders.Count} lệnh.";
    }

    [RelayCommand]
    private async Task LoadPeriod()
    {
        CustomerRows.Clear();
        foreach (var r in await dashboard.ByCustomerAsync(From, To)) CustomerRows.Add(r);
        VehicleRows.Clear();
        foreach (var r in await dashboard.ByVehicleAsync(From, To)) VehicleRows.Add(r);
        Status = $"Từ {From:dd/MM} đến {To:dd/MM} · {CustomerRows.Sum(r => r.Trips)} chuyến · {CustomerRows.Sum(r => r.Freight):N0}";
    }
}
