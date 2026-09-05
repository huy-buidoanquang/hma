using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Services;
using Hma.Domain.Entities;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class CashReceiptWorkspaceViewModel(
    CashDocumentService cash,
    CustomerService customers,
    DispatchOrderService orders,
    CompanyService company,
    IDocumentPrinter printer,
    IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private CashReceipt editor = new();
    [ObservableProperty] private CashReceipt? selected;
    public ObservableCollection<CashReceipt> Items { get; } = [];
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<DispatchOrder> DispatchOrders { get; } = [];

    public override async Task LoadAsync()
    {
        UsePrompt(prompt);
        Customers.Clear();
        foreach (var c in await customers.SearchAsync(null, null, null, null)) Customers.Add(c);
        DispatchOrders.Clear();
        foreach (var d in await orders.SearchAsync(null, null, null, null, null, null, null, null, null)) DispatchOrders.Add(d);
        await Search();
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var r in await cash.SearchReceiptsAsync(null, null, null)) Items.Add(r);
    }

    [RelayCommand]
    private async Task NewItem()
    {
        Editor = await cash.NewReceiptAsync();
        Selected = null;
        EnterCreate("Thêm phiếu thu", Editor);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = Selected;
        EnterExisting($"Xem phiếu thu — {Editor.Code}", $"Sửa phiếu thu — {Editor.Code}", Editor);
    }

    [RelayCommand]
    private async Task Save()
    {
        await RunAsync(async () =>
        {
            await cash.SaveReceiptAsync(Editor);
            await Search();
        }, "Đã lưu phiếu thu.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Print()
    {
        if (Editor.Id == 0) return;
        var companyInfo = await company.GetAsync();
        Process.Start(new ProcessStartInfo(printer.PrintCashReceipt(Editor, companyInfo)) { UseShellExecute = true });
    }
}
