using Hma.Desktop.Wpf.Abstractions;
using Hma.Application.Abstractions.Reporting;
using Hma.Application.Features.Accounting;
using Hma.Application.Features.Customers;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Settings;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Domain.Entities;
using Hma.Desktop.Wpf.Presentation.Features.Accounting.Models;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.Presentation.Features.Accounting.ViewModels;

public partial class CashPaymentWorkspaceViewModel(
    CashDocumentService cash,
    CustomerService customers,
    CatalogOptionQueryService catalog,
    CompanyService company,
    IDocumentRenderer printer,
    IDocumentInteractionService documentInteraction,
    IUserPrompt prompt, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private CashPaymentModel editor = new();
    [ObservableProperty] private CashPaymentModel? selected;
    public ObservableCollection<CashPaymentModel> Items { get; } = [];
    public ObservableCollection<CustomerSummary> Customers { get; } = [];
    public ObservableCollection<EmployeeOption> Drivers { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(
        Editor.Id, Editor.Code, Editor.DocumentDate, Editor.Kind, Editor.CustomerId,
        Editor.DriverEmployeeId, Editor.Amount, Editor.PayeeName, Editor.Address, Editor.Reason, Editor.EmployeeId);

    public override async Task LoadAsync()
    {
        UsePrompt(prompt);
        Customers.Clear();
        foreach (var c in await customers.SearchAsync(null, null, null, null)) Customers.Add(c);
        Drivers.Clear();
        foreach (var d in await catalog.EmployeesAsync()) Drivers.Add(d);
        await Search();
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var p in await cash.SearchPaymentsAsync(null, null, null)) Items.Add(CashPaymentModel.From(p));
    }

    [RelayCommand]
    private async Task NewItem()
    {
        Editor = CashPaymentModel.From(await cash.NewPaymentAsync());
        Selected = null;
        EnterCreateState("Thêm phiếu chi", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = Selected.Copy();
        EnterExistingState($"Xem phiếu chi — {Editor.Code}", $"Sửa phiếu chi — {Editor.Code}", CaptureEditorState);
    }

    [RelayCommand]
    private async Task Save()
    {
        await RunAsync(async () =>
        {
            var saved = await cash.SavePaymentAsync(Editor.ToCommand());
            Editor = CashPaymentModel.From(saved);
            await Search();
        }, "Đã lưu phiếu chi.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Print()
    {
        if (Editor.Id == 0) return;
        var companyInfo = await company.GetAsync();
        await documentInteraction.OpenAsync(printer.PrintCashPayment(Editor.ToDetails(), companyInfo));
    }
}
