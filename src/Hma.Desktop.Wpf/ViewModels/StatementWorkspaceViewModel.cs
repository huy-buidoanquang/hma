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

public partial class StatementWorkspaceViewModel(
    FreightStatementService statements,
    CustomerService customers,
    CompanyService company,
    IDocumentPrinter printer,
    ICurrentUser user) : WorkspaceBase
{
    [ObservableProperty] private int? customerId;
    [ObservableProperty] private int year = DateTime.Today.Year;
    [ObservableProperty] private int month = DateTime.Today.Month;
    [ObservableProperty] private FreightStatement? current;
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<FreightStatement> Items { get; } = [];
    public ObservableCollection<FreightStatementLine> Lines { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Statements);
        Customers.Clear();
        foreach (var c in await customers.SearchAsync(null, null, null, null))
            if (!c.IsWalkIn) Customers.Add(c);
        Items.Clear();
        foreach (var s in await statements.ListAsync(null)) Items.Add(s);
    }

    [RelayCommand]
    private async Task Generate()
    {
        if (CustomerId is null) { ShowToast("Chọn khách hàng.", isError: true); return; }
        await RunAsync(async () =>
        {
            Current = await statements.GenerateAsync(CustomerId.Value, Year, Month);
            Lines.Clear();
            if (Current is not null)
                foreach (var l in Current.Lines) Lines.Add(l);
            Status = Current is null ? "Không có dữ liệu." : $"Bảng kê {Current.Code}: {Current.TripCount} chuyến, {Current.GrandTotal:N0}.";
            await LoadAsync();
        });
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        if (Current is null) return;
        var companyInfo = await company.GetAsync();
        var path = printer.PrintFreightStatement(Current, companyInfo);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (Current is null) return;
        var path = Path.Combine(Path.GetTempPath(), $"BK-{Current.Code}.xlsx");
        printer.ExportFreightStatementExcel(Current, path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
