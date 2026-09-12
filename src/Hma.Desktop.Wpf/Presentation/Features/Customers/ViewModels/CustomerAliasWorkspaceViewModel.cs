using Hma.Application.Features.Customers;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Customers.Models;

namespace Hma.Desktop.Wpf.Presentation.Features.Customers.ViewModels;

public partial class CustomerAliasWorkspaceViewModel(
    CustomerAliasService aliases,
    CustomerService customers,
    ICurrentUser user,
    IUserPrompt prompt, IUiOperationGate operationGate) : WorkspaceBase(operationGate)
{
    [ObservableProperty] private string? filterText;
    [ObservableProperty] private CustomerAliasEditorModel editor = new();
    [ObservableProperty] private CustomerAliasSummary? selected;
    public ObservableCollection<CustomerAliasSummary> Items { get; } = [];
    public ObservableCollection<CustomerSummary> Customers { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(Editor.Id, Editor.Alias, Editor.CustomerId);

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
        Editor = new CustomerAliasEditorModel();
        EnterCreateState("Thêm bí danh khách", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = CustomerAliasEditorModel.From(Selected);
        EnterExistingState($"Xem bí danh — {Editor.Alias}", $"Sửa bí danh — {Editor.Alias}", CaptureEditorState);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await aliases.SaveAsync(Editor.ToCommand());
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
