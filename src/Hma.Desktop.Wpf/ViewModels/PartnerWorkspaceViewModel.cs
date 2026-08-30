using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class PartnerWorkspaceViewModel(CatalogService catalog, ICurrentUser user, IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private string? filterCode;
    [ObservableProperty] private string? filterName;
    [ObservableProperty] private Partner? selected;
    [ObservableProperty] private Partner editor = new();
    public ObservableCollection<Partner> Items { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Partners);
        UsePrompt(prompt);
        await Search();
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var p in await catalog.PartnersAsync(FilterCode, FilterName)) Items.Add(p);
        Status = $"{Items.Count} đối tác";
    }

    [RelayCommand]
    private async Task ResetFilters()
    {
        FilterCode = FilterName = null;
        await Search();
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new Partner();
        EnterCreate("Thêm đối tác");
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = new Partner
        {
            Id = Selected.Id, Code = Selected.Code, Name = Selected.Name, TaxCode = Selected.TaxCode,
            Address = Selected.Address, ContactName = Selected.ContactName, Phone = Selected.Phone, Email = Selected.Email
        };
        EnterEdit($"Sửa đối tác — {Editor.Code}");
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await catalog.SavePartnerAsync(Editor);
            await Search();
        }, "Đã lưu đối tác.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await catalog.DeleteAsync<Partner>(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }
}
