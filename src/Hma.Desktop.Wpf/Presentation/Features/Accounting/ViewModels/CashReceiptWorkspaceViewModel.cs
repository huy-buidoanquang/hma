using Hma.Desktop.Wpf.Abstractions;
using Hma.Application.Abstractions.Reporting;
using Hma.Application.Features.Dispatching;
using Hma.Application.Features.Accounting;
using Hma.Application.Features.Customers;
using Hma.Application.Features.Settings;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Domain.Entities;
using Hma.Desktop.Wpf.Presentation.Features.Accounting.Models;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.Presentation.Features.Accounting.ViewModels;

public partial class CashReceiptWorkspaceViewModel(
    CashDocumentService cash,
    CustomerService customers,
    DispatchOrderQueryService dispatchQueries,
    CompanyService company,
    IDocumentRenderer printer,
    IDocumentInteractionService documentInteraction,
    IUserPrompt prompt, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private CashReceiptModel editor = new();
    [ObservableProperty] private CashReceiptModel? selected;
    public ObservableCollection<CashReceiptModel> Items { get; } = [];
    public ObservableCollection<CustomerSummary> Customers { get; } = [];
    public ObservableCollection<DispatchOrderSummary> DispatchOrders { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(
        Editor.Id, Editor.Code, Editor.DocumentDate, Editor.Kind, Editor.DispatchOrderId,
        Editor.CustomerId, Editor.Amount, Editor.PayerName, Editor.Address, Editor.Reason, Editor.EmployeeId);

    public override async Task LoadAsync()
    {
        UsePrompt(prompt);
        Customers.Clear();
        foreach (var c in await customers.SearchAsync(null, null, null, null)) Customers.Add(c);
        DispatchOrders.Clear();
        foreach (var d in await dispatchQueries.SearchAsync(null, null, null, null, null, null, null, null, null)) DispatchOrders.Add(d);
        await Search();
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var r in await cash.SearchReceiptsAsync(null, null, null)) Items.Add(CashReceiptModel.From(r));
    }

    [RelayCommand]
    private async Task NewItem()
    {
        Editor = CashReceiptModel.From(await cash.NewReceiptAsync());
        Selected = null;
        EnterCreateState("Thêm phiếu thu", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = Selected.Copy();
        EnterExistingState($"Xem phiếu thu — {Editor.Code}", $"Sửa phiếu thu — {Editor.Code}", CaptureEditorState);
    }

    [RelayCommand]
    private async Task Save()
    {
        await RunAsync(async () =>
        {
            var saved = await cash.SaveReceiptAsync(Editor.ToCommand());
            Editor = CashReceiptModel.From(saved);
            await Search();
        }, "Đã lưu phiếu thu.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Print()
    {
        if (Editor.Id == 0) return;
        var companyInfo = await company.GetAsync();
        await documentInteraction.OpenAsync(printer.PrintCashReceipt(Editor.ToDetails(), companyInfo));
    }
}
