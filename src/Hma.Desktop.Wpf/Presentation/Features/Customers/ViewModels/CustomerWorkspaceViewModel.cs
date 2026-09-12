using Hma.Desktop.Wpf.Abstractions;
using Hma.Application.Abstractions.Reporting;
using Hma.Application.Features.Customers;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Customers.Models;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.Presentation.Features.Customers.ViewModels;

public partial class CustomerWorkspaceViewModel(
    CustomerService customers,
    CatalogOptionQueryService catalog,
    ICurrentUser user,
    IUserPrompt prompt,
    IDocumentRenderer printer,
    IDocumentInteractionService documentInteraction, IUiOperationGate operationGate) : WorkspaceBase(operationGate)
{
    [ObservableProperty] private string? filterCode;
    [ObservableProperty] private string? filterName;
    [ObservableProperty] private string? filterAddress;
    [ObservableProperty] private string? filterTaxCode;
    [ObservableProperty] private DateTime? filterFrom;
    [ObservableProperty] private DateTime? filterTo;
    [ObservableProperty] private CustomerSummary? selected;
    [ObservableProperty] private CustomerEditorModel editor = new();
    public ObservableCollection<CustomerSummary> Items { get; } = [];
    public ObservableCollection<CatalogOption> Cities { get; } = [];
    public ObservableCollection<EmployeeOption> Accountants { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(
        Editor.Id, Editor.Code, Editor.Name, Editor.Address, Editor.Phone, Editor.TaxCode,
        Editor.ContactName, Editor.Email, Editor.CityId, Editor.AccountantEmployeeId, Editor.IsWalkIn);

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
        Editor = new CustomerEditorModel();
        EnterCreateState("Thêm khách hàng", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = CustomerEditorModel.From(Selected);
        EnterExistingState($"Xem khách hàng — {Editor.Code}", $"Sửa khách hàng — {Editor.Code}", CaptureEditorState);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await customers.SaveAsync(Editor.ToCommand());
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
    private async Task ExportExcel()
    {
        if (!CanPrint) return;
        await documentInteraction.OpenAsync(printer.ExportCustomersExcel(Items.ToList()));
        Status = "Đã xuất Excel danh sách khách.";
    }
}
