using Hma.Desktop.Wpf.Abstractions;
using Hma.Application.Abstractions.Reporting;
using Hma.Application.Features.Accounting;
using Hma.Application.Features.Customers;
using Hma.Application.Features.Settings;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Domain.Entities;
using Hma.Desktop.Wpf.Presentation.Features.Accounting.Models;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.Presentation.Features.Accounting.ViewModels;

public partial class InvoiceWorkspaceViewModel(
    VatInvoiceService invoices,
    CustomerService customers,
    CompanyService company,
    IDocumentRenderer printer,
    IDocumentInteractionService documentInteraction,
    IUserPrompt prompt, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private VatInvoiceModel editor = new();
    [ObservableProperty] private VatInvoiceModel? selected;
    [ObservableProperty] private InvoiceOrderModel? availableSelected;
    public ObservableCollection<VatInvoiceModel> Items { get; } = [];
    public ObservableCollection<CustomerSummary> Customers { get; } = [];
    public ObservableCollection<InvoiceOrderModel> Available { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(
        Editor.Id, Editor.Code, Editor.InvoiceDate, Editor.CustomerId, Editor.EmployeeId,
        Editor.IsByCustomer, Editor.GoodsName, Editor.PaymentMethodText, Editor.Unit,
        Editor.Quantity, Editor.Amount, Editor.VatRate, Editor.VatAmount, Editor.TotalAmount,
        string.Join(";", Editor.Lines.Select(line => line.Id)));

    public override async Task LoadAsync()
    {
        UsePrompt(prompt);
        Customers.Clear();
        foreach (var c in await customers.SearchAsync(null, null, null, null)) Customers.Add(c);
        Items.Clear();
        foreach (var i in await invoices.SearchAsync(null, null, null)) Items.Add(VatInvoiceModel.From(i));
    }

    [RelayCommand]
    private async Task NewItem()
    {
        Editor = VatInvoiceModel.From(await invoices.CreateNewAsync());
        await RefreshAvailable();
        Selected = null;
        EnterCreateState("Thêm hóa đơn", CaptureEditorState);
    }

    [RelayCommand]
    private async Task RefreshAvailable()
    {
        Available.Clear();
        foreach (var d in await invoices.AvailableOrdersAsync(Editor.IsByCustomer ? Editor.CustomerId : null, null, null))
            Available.Add(InvoiceOrderModel.From(d));
    }

    [RelayCommand]
    private void AddLine()
    {
        if (AvailableSelected is null) return;
        Editor.Lines.Add(AvailableSelected);
        Editor.TotalAmount = Editor.Lines.Sum(l => l.TotalAmount);
        Editor.RecalculateFromTotal();
        OnPropertyChanged(nameof(Editor));
    }

    [RelayCommand]
    private async Task ViewItem()
    {
        if (Selected is null) return;
        await Open(Selected.Id);
        EnterExistingState($"Xem hóa đơn — {Editor.Code}", $"Sửa hóa đơn — {Editor.Code}", CaptureEditorState);
    }

    private async Task Open(int id)
    {
        var full = await invoices.GetAsync(id);
        if (full is not null) Editor = VatInvoiceModel.From(full);
    }

    [RelayCommand]
    private async Task Save()
    {
        await RunAsync(async () =>
        {
            var saved = await invoices.SaveAsync(Editor.ToCommand());
            Editor = VatInvoiceModel.From(saved);
            await LoadAsync();
        }, "Đã lưu hóa đơn.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Print()
    {
        if (Editor.Id == 0) return;
        var companyInfo = await company.GetAsync();
        await documentInteraction.OpenAsync(printer.PrintVatInvoice(Editor.ToDetails(), companyInfo));
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        var list = await invoices.SearchAsync(null, null, null);
        await documentInteraction.OpenAsync(printer.ExportVatExcel(list));
    }
}
