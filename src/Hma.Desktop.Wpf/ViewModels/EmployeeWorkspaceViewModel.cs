using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class EmployeeWorkspaceViewModel(CatalogService catalog, ICurrentUser user, IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private Employee? selected;
    [ObservableProperty] private Employee editor = new();
    public ObservableCollection<Employee> Items { get; } = [];
    public ObservableCollection<Department> Departments { get; } = [];
    public ObservableCollection<JobTitle> JobTitles { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Employees);
        UsePrompt(prompt);
        await RunAsync(async () =>
        {
            Departments.Clear();
            foreach (var d in await catalog.DepartmentsAsync()) Departments.Add(d);
            JobTitles.Clear();
            foreach (var j in await catalog.JobTitlesAsync()) JobTitles.Add(j);
            Items.Clear();
            foreach (var e in await catalog.EmployeesAsync()) Items.Add(e);
            Status = $"{Items.Count} nhân viên";
        });
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new Employee();
        EnterCreate("Thêm nhân viên", Editor);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = new Employee
        {
            Id = Selected.Id,
            Code = Selected.Code,
            Name = Selected.Name,
            Address = Selected.Address,
            Phone = Selected.Phone,
            Mobile = Selected.Mobile,
            BirthDate = Selected.BirthDate,
            IdentityNumber = Selected.IdentityNumber,
            VehiclePlate = Selected.VehiclePlate,
            DepartmentId = Selected.DepartmentId,
            JobTitleId = Selected.JobTitleId
        };
        EnterExisting($"Xem nhân viên — {Editor.Code}", $"Sửa nhân viên — {Editor.Code}", Editor);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await catalog.SaveEmployeeAsync(Editor);
            await LoadAsync();
        }, "Đã lưu nhân viên.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await catalog.DeleteAsync<Employee>(Editor.Id);
            await LoadAsync();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }
}
