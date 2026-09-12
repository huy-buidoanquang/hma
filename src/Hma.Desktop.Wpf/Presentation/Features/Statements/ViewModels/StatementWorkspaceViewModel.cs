using Hma.Desktop.Wpf.Abstractions;
using Hma.Application.Abstractions.Reporting;
using Hma.Application.Features.Customers;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Statements;
using Hma.Application.Common.Authorization;
using Hma.Application.Features.Settings;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Domain.Entities;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.Presentation.Features.Statements.ViewModels;

public partial class StatementWorkspaceViewModel(
    FreightStatementService statements,
    CustomerService customers,
    CompanyService company,
    IDocumentRenderer printer,
    IDocumentInteractionService documentInteraction,
    ICurrentUser user, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private int? customerId;
    [ObservableProperty] private int year = DateTime.Today.Year;
    [ObservableProperty] private int month = DateTime.Today.Month;
    [ObservableProperty] private FreightStatementDetails? current;
    [ObservableProperty] private FreightStatementDetails? selected;
    [ObservableProperty] private string voidReason = "";
    public ObservableCollection<CustomerSummary> Customers { get; } = [];
    public ObservableCollection<FreightStatementDetails> Items { get; } = [];
    public ObservableCollection<FreightStatementLineSummary> Lines { get; } = [];

    public bool CanGenerate => CanCreate && (Current is null || Current.Status == FinancialDocumentStatus.Draft);
    public bool CanSubmit => CanUpdate && Current?.Status == FinancialDocumentStatus.Draft && Current.Lines.Count > 0;
    public bool CanFinalize =>
        CanUpdate && Current?.Status == FinancialDocumentStatus.Submitted
        && Current.SubmittedByUserId != user.UserId;
    public bool CanVoid =>
        CanUpdate && user.IsManager && Current?.Status == FinancialDocumentStatus.Finalized;

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Statements);
        Customers.Clear();
        foreach (var c in await customers.SearchAsync(null, null, null, null))
            if (!c.IsWalkIn) Customers.Add(c);
        Items.Clear();
        foreach (var s in await statements.ListDetailsAsync(null)) Items.Add(s);
    }

    partial void OnSelectedChanged(FreightStatementDetails? value)
    {
        if (value is not null)
            _ = OpenAsync(value.Id);
    }

    partial void OnCurrentChanged(FreightStatementDetails? value)
    {
        NotifyActions();
    }

    private void RefreshCurrent(FreightStatementDetails? value)
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
            RefreshCurrent(await statements.GetDetailsAsync(id));
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
            RefreshCurrent(await statements.GenerateDetailsAsync(CustomerId.Value, Year, Month));
            Lines.Clear();
            if (Current is not null)
                foreach (var l in Current.Lines) Lines.Add(l);
            await LoadAsync();
        }, "Đã lập bảng kê tháng.");
    }

    [RelayCommand]
    private async Task Submit()
    {
        if (!CanSubmit || Current is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrent(await statements.SubmitDetailsAsync(Current.Id));
            await LoadAsync();
        }, "Đã gửi duyệt bảng kê.");
    }

    [RelayCommand]
    private async Task FinalizeStatement()
    {
        if (!CanFinalize || Current is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrent(await statements.FinalizeDetailsAsync(Current.Id));
            await LoadAsync();
        }, "Đã chốt bảng kê.");
    }

    [RelayCommand]
    private async Task VoidStatement()
    {
        if (!CanVoid || Current is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrent(await statements.VoidDetailsAsync(Current.Id, VoidReason));
            await LoadAsync();
        }, "Đã hủy bảng kê.");
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        if (Current is null) return;
        var companyInfo = await company.GetAsync();
        await documentInteraction.OpenAsync(printer.PrintFreightStatement(Current, companyInfo));
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        if (Current is null) return;
        await documentInteraction.OpenAsync(printer.ExportFreightStatementExcel(Current));
    }
}
