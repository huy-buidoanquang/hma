using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class CustomerAliasWorkspaceViewModel(
    CustomerAliasService aliases,
    CustomerService customers,
    ICurrentUser user,
    IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private string? filterText;
    [ObservableProperty] private CustomerAlias editor = new();
    [ObservableProperty] private CustomerAlias? selected;
    public ObservableCollection<CustomerAlias> Items { get; } = [];
    public ObservableCollection<Customer> Customers { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Settings);
        UsePrompt(prompt);
        DiscardUnsavedEditor();
        Customers.Clear();
        foreach (var customer in await customers.SearchAsync(null, null, null, null))
            Customers.Add(customer);
        await Search();
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var row in await aliases.ListAsync())
        {
            if (string.IsNullOrWhiteSpace(FilterText)
                || row.Alias.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
                || (row.Customer?.Code.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (row.Customer?.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false))
                Items.Add(row);
        }
        Status = $"{Items.Count} bí danh khách";
    }

    [RelayCommand]
    private async Task ResetFilters()
    {
        FilterText = null;
        await Search();
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new CustomerAlias();
        EnterCreate("Thêm bí danh khách", Editor);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = new CustomerAlias
        {
            Id = Selected.Id,
            Alias = Selected.Alias,
            CustomerId = Selected.CustomerId
        };
        EnterExisting($"Xem bí danh — {Editor.Alias}", $"Sửa bí danh — {Editor.Alias}", Editor);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await aliases.SaveAsync(Editor);
            await Search();
        }, "Đã lưu bí danh khách.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await aliases.DeleteAsync(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }
}
