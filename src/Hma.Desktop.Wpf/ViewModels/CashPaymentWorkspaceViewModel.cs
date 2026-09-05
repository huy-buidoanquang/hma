using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Services;
using Hma.Domain.Entities;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class CashPaymentWorkspaceViewModel(
    CashDocumentService cash,
    CustomerService customers,
    CatalogService catalog,
    CompanyService company,
    IDocumentPrinter printer,
    IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private CashPayment editor = new();
    [ObservableProperty] private CashPayment? selected;
    public ObservableCollection<CashPayment> Items { get; } = [];
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Employee> Drivers { get; } = [];

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
        foreach (var p in await cash.SearchPaymentsAsync(null, null, null)) Items.Add(p);
    }

    [RelayCommand]
    private async Task NewItem()
    {
        Editor = await cash.NewPaymentAsync();
        Selected = null;
        EnterCreate("Thêm phiếu chi", Editor);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = Selected;
        EnterExisting($"Xem phiếu chi — {Editor.Code}", $"Sửa phiếu chi — {Editor.Code}", Editor);
    }

    [RelayCommand]
    private async Task Save()
    {
        await RunAsync(async () =>
        {
            await cash.SavePaymentAsync(Editor);
            await Search();
        }, "Đã lưu phiếu chi.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Print()
    {
        if (Editor.Id == 0) return;
        var companyInfo = await company.GetAsync();
        Process.Start(new ProcessStartInfo(printer.PrintCashPayment(Editor, companyInfo)) { UseShellExecute = true });
    }
}
