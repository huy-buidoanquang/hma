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

public partial class CustomerWorkspaceViewModel(
    CustomerService customers,
    CatalogService catalog,
    ICurrentUser user,
    IUserPrompt prompt,
    IDocumentPrinter printer) : WorkspaceBase
{
    [ObservableProperty] private string? filterCode;
    [ObservableProperty] private string? filterName;
    [ObservableProperty] private string? filterAddress;
    [ObservableProperty] private string? filterTaxCode;
    [ObservableProperty] private DateTime? filterFrom;
    [ObservableProperty] private DateTime? filterTo;
    [ObservableProperty] private Customer? selected;
    [ObservableProperty] private Customer editor = new();
    public ObservableCollection<Customer> Items { get; } = [];
    public ObservableCollection<City> Cities { get; } = [];
    public ObservableCollection<Employee> Accountants { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Customers);
        UsePrompt(prompt);
        await RunAsync(async () =>
        {
            Cities.Clear();
            foreach (var c in await catalog.CitiesAsync()) Cities.Add(c);
            Accountants.Clear();
            foreach (var e in await catalog.EmployeesAsync()) Accountants.Add(e);
            await Search();
        });
    }

    [RelayCommand]
    private async Task Search()
    {
        await RunAsync(async () =>
        {
            Items.Clear();
            foreach (var c in await customers.SearchAsync(FilterCode, FilterName, FilterAddress, FilterTaxCode, FilterFrom, FilterTo))
                Items.Add(c);
            Status = $"Tìm thấy {Items.Count} khách hàng";
        });
    }

    [RelayCommand]
    private async Task ResetFilters()
    {
        FilterCode = FilterName = FilterAddress = FilterTaxCode = null;
        FilterFrom = FilterTo = null;
        await Search();
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new Customer { UpdatedAt = DateTime.Today };
        EnterCreate("Thêm khách hàng");
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = new Customer
        {
            Id = Selected.Id,
            Code = Selected.Code,
            Name = Selected.Name,
            Address = Selected.Address,
            Phone = Selected.Phone,
            TaxCode = Selected.TaxCode,
            ContactName = Selected.ContactName,
            Email = Selected.Email,
            CityId = Selected.CityId,
            AccountantEmployeeId = Selected.AccountantEmployeeId,
            IsWalkIn = Selected.IsWalkIn
        };
        EnterEdit($"Sửa khách hàng — {Editor.Code}");
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await customers.SaveAsync(Editor);
            await Search();
        }, "Đã lưu khách hàng.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await customers.DeleteAsync(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (!CanPrint) return;
        var path = Path.Combine(Path.GetTempPath(), "KH.xlsx");
        printer.ExportCustomersExcel(Items.ToList(), path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Status = "Đã xuất Excel danh sách khách.";
    }
}
