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
    [ObservableProperty] private FreightStatement? selected;
    [ObservableProperty] private string voidReason = "";
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<FreightStatement> Items { get; } = [];
    public ObservableCollection<FreightStatementLine> Lines { get; } = [];

    public bool CanGenerate => CanCreate && (Current is null || Current.Status == FinancialDocumentStatus.Draft);
    public bool CanSubmit => CanUpdate && Current?.Status == FinancialDocumentStatus.Draft && Current.Lines.Count > 0;
    public bool CanFinalize =>
        CanUpdate && Current?.Status == FinancialDocumentStatus.Submitted
        && Current.SubmittedByUserId != user.User?.Id;
    public bool CanVoid =>
        CanUpdate && user.User?.IsManager == true && Current?.Status == FinancialDocumentStatus.Finalized;

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Statements);
        Customers.Clear();
        foreach (var c in await customers.SearchAsync(null, null, null, null))
            if (!c.IsWalkIn) Customers.Add(c);
        Items.Clear();
        foreach (var s in await statements.ListAsync(null)) Items.Add(s);
    }

    partial void OnSelectedChanged(FreightStatement? value)
    {
        if (value is not null)
            _ = OpenAsync(value.Id);
    }

    partial void OnCurrentChanged(FreightStatement? value)
    {
        NotifyActions();
    }

    private void RefreshCurrent(FreightStatement? value)
    {
        Current = value;
        OnPropertyChanged(nameof(Current));
        NotifyActions();
    }

    private void NotifyActions()
    {
        OnPropertyChanged(nameof(CanGenerate));
        OnPropertyChanged(nameof(CanSubmit));
        OnPropertyChanged(nameof(CanFinalize));
        OnPropertyChanged(nameof(CanVoid));
    }

    private async Task OpenAsync(int id)
    {
        await RunAsync(async () =>
        {
            RefreshCurrent(await statements.GetAsync(id));
            Lines.Clear();
            if (Current is not null)
                foreach (var line in Current.Lines.OrderBy(x => x.TripDate)) Lines.Add(line);
            VoidReason = Current?.VoidReason ?? "";
        });
    }

    [RelayCommand]
    private async Task Generate()
    {
        if (CustomerId is null) { ShowToast("Chọn khách hàng.", isError: true); return; }
        await RunAsync(async () =>
        {
            RefreshCurrent(await statements.GenerateAsync(CustomerId.Value, Year, Month));
            Lines.Clear();
            if (Current is not null)
                foreach (var l in Current.Lines) Lines.Add(l);
            Status = Current is null ? "Không có dữ liệu." : $"Bảng kê {Current.Code}: {Current.TripCount} chuyến, {Current.GrandTotal:N0}.";
            await LoadAsync();
        });
    }

    [RelayCommand]
    private async Task Submit()
    {
        if (!CanSubmit || Current is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrent(await statements.SubmitAsync(Current.Id));
            Status = "Đã gửi duyệt bảng kê.";
            await LoadAsync();
        });
    }

    [RelayCommand]
    private async Task FinalizeStatement()
    {
        if (!CanFinalize || Current is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrent(await statements.FinalizeAsync(Current.Id));
            Status = "Đã chốt bảng kê.";
            await LoadAsync();
        });
    }

    [RelayCommand]
    private async Task VoidStatement()
    {
        if (!CanVoid || Current is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrent(await statements.VoidAsync(Current.Id, VoidReason));
            Status = "Đã hủy bảng kê.";
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
        var path = TemporaryReportFile.Create($"BK-{Current.Code}.xlsx");
        printer.ExportFreightStatementExcel(Current, path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
