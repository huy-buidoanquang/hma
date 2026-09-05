using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class CityWorkspaceViewModel(CatalogService catalog, ICurrentUser user, IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private string? filterName;
    [ObservableProperty] private City editor = new();
    [ObservableProperty] private City? selected;
    public ObservableCollection<City> Items { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Cities);
        UsePrompt(prompt);
        await Search();
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var c in await catalog.CitiesAsync())
            if (string.IsNullOrWhiteSpace(FilterName) || c.Code.Contains(FilterName, StringComparison.OrdinalIgnoreCase)
                || c.Name.Contains(FilterName, StringComparison.OrdinalIgnoreCase))
                Items.Add(c);
        Status = $"{Items.Count} thành phố";
    }

    [RelayCommand]
    private async Task ResetFilters()
    {
        FilterName = null;
        await Search();
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new City();
        EnterCreate("Thêm thành phố", Editor);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = new City { Id = Selected.Id, Code = Selected.Code, Name = Selected.Name, Description = Selected.Description };
        EnterExisting($"Xem thành phố — {Editor.Code}", $"Sửa thành phố — {Editor.Code}", Editor);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await catalog.SaveCityAsync(Editor);
            await Search();
        }, "Đã lưu thành phố.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await catalog.DeleteAsync<City>(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }
}
