using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Services;
using Hma.Domain.Entities;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class InvoiceWorkspaceViewModel(
    VatInvoiceService invoices,
    CustomerService customers,
    CompanyService company,
    IDocumentPrinter printer,
    IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private VatInvoice editor = new();
    [ObservableProperty] private VatInvoice? selected;
    [ObservableProperty] private DispatchOrder? availableSelected;
    public ObservableCollection<VatInvoice> Items { get; } = [];
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<DispatchOrder> Available { get; } = [];

    public override async Task LoadAsync()
    {
        UsePrompt(prompt);
        Customers.Clear();
        foreach (var c in await customers.SearchAsync(null, null, null, null)) Customers.Add(c);
        Items.Clear();
        foreach (var i in await invoices.SearchAsync(null, null, null)) Items.Add(i);
    }

    [RelayCommand]
    private async Task NewItem()
    {
        Editor = await invoices.CreateNewAsync();
        await RefreshAvailable();
        Selected = null;
        EnterCreate("Thêm hóa đơn", Editor, Editor.Lines);
    }

    [RelayCommand]
    private async Task RefreshAvailable()
    {
        Available.Clear();
        foreach (var d in await invoices.AvailableOrdersAsync(Editor.IsByCustomer ? Editor.CustomerId : null, null, null))
            Available.Add(d);
    }

    [RelayCommand]
    private void AddLine()
    {
        if (AvailableSelected is null) return;
        Editor.Lines.Add(new VatInvoiceLine { DispatchOrderId = AvailableSelected.Id, DispatchOrder = AvailableSelected });
        Editor.TotalAmount = Editor.Lines.Sum(l => l.DispatchOrder?.TotalAmount ?? 0);
        Editor.RecalculateFromTotal();
        OnPropertyChanged(nameof(Editor));
    }

    [RelayCommand]
    private async Task ViewItem()
    {
        if (Selected is null) return;
        await Open(Selected.Id);
        EnterExisting($"Xem hóa đơn — {Editor.Code}", $"Sửa hóa đơn — {Editor.Code}", Editor, Editor.Lines);
    }

    private async Task Open(int id)
    {
        var full = await invoices.GetAsync(id);
        if (full is not null) Editor = full;
    }

    [RelayCommand]
    private async Task Save()
    {
        await RunAsync(async () =>
        {
            await invoices.SaveAsync(Editor);
            await LoadAsync();
        }, "Đã lưu hóa đơn.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Print()
    {
        if (Editor.Id == 0) return;
        var companyInfo = await company.GetAsync();
        Process.Start(new ProcessStartInfo(printer.PrintVatInvoice(Editor, companyInfo)) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        var list = await invoices.SearchAsync(null, null, null);
        var path = TemporaryReportFile.Create("HDGTGT.xlsx");
        printer.ExportVatExcel(list, path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
