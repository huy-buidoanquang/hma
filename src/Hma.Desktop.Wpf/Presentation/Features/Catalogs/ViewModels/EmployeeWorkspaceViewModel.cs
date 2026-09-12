using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Catalogs.Models;

namespace Hma.Desktop.Wpf.Presentation.Features.Catalogs.ViewModels;

public partial class EmployeeWorkspaceViewModel(EmployeeService employees, CatalogOptionQueryService catalog, ICurrentUser user, IUserPrompt prompt, IUiOperationGate operationGate) : WorkspaceBase(operationGate)
{
    [ObservableProperty] private EmployeeSummary? selected;
    [ObservableProperty] private EmployeeEditorModel editor = new();
    public ObservableCollection<EmployeeSummary> Items { get; } = [];
    public ObservableCollection<CatalogOption> Departments { get; } = [];
    public ObservableCollection<CatalogOption> JobTitles { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(
        Editor.Id, Editor.Code, Editor.Name, Editor.Address, Editor.Phone, Editor.Mobile,
        Editor.BirthDate, Editor.IdentityNumber, Editor.VehiclePlate, Editor.DepartmentId, Editor.JobTitleId);

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
            foreach (var e in await employees.ListAsync()) Items.Add(e);
            Status = $"{Items.Count} nhân viên";
        });
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new EmployeeEditorModel();
        EnterCreateState("Thêm nhân viên", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = EmployeeEditorModel.From(Selected);
        EnterExistingState($"Xem nhân viên — {Editor.Code}", $"Sửa nhân viên — {Editor.Code}", CaptureEditorState);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await employees.SaveAsync(Editor.ToCommand());
            await LoadAsync();
        }, "Đã lưu nhân viên.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await employees.DeleteAsync(Editor.Id);
            await LoadAsync();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }
}
