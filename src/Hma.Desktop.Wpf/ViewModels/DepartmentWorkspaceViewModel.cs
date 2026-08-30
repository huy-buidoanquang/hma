using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class DepartmentWorkspaceViewModel(CatalogService catalog, ICurrentUser user, IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private Department? selected;
    [ObservableProperty] private Department editor = new();
    public ObservableCollection<Department> Items { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Departments);
        UsePrompt(prompt);
        Items.Clear();
        foreach (var d in await catalog.DepartmentsAsync()) Items.Add(d);
        Status = $"{Items.Count} phòng ban";
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new Department();
        EnterCreate("Thêm phòng ban");
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = new Department { Id = Selected.Id, Code = Selected.Code, Name = Selected.Name };
        EnterEdit($"Sửa phòng ban — {Editor.Code}");
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await catalog.SaveDepartmentAsync(Editor);
            await LoadAsync();
        }, "Đã lưu phòng ban.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await catalog.DeleteAsync<Department>(Editor.Id);
            await LoadAsync();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }
}
