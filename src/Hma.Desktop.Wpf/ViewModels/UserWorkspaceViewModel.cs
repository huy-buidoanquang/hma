using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class UserWorkspaceViewModel(UserAdminService users, CatalogService catalog, ICurrentUser user, IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private AppUser editor = new();
    [ObservableProperty] private AppUser? selected;
    [ObservableProperty] private string newPassword = "";
    public ObservableCollection<AppUser> Items { get; } = [];
    public ObservableCollection<PermissionRow> PermissionRows { get; } = [];
    public ObservableCollection<Employee> Employees { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Users);
        UsePrompt(prompt);
        Employees.Clear();
        foreach (var e in await catalog.EmployeesAsync()) Employees.Add(e);
        Items.Clear();
        foreach (var u in await users.ListAsync()) Items.Add(u);
        var screens = await users.ScreensAsync();
        PermissionRows.Clear();
        foreach (var s in screens)
            PermissionRows.Add(new PermissionRow { ScreenId = s.Id, ScreenName = s.Name, ScreenKey = s.Key });
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new AppUser();
        NewPassword = "";
        foreach (var row in PermissionRows)
            row.CanView = row.CanCreate = row.CanUpdate = row.CanDelete = row.CanPrint = false;
        EnterCreate("Thêm người dùng", Editor, new LiveValue(() => NewPassword), PermissionRows);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        ApplyUser(Selected);
        EnterExisting($"Xem người dùng — {Editor.UserName}", $"Sửa người dùng — {Editor.UserName}",
            Editor, new LiveValue(() => NewPassword), PermissionRows);
    }

    partial void OnSelectedChanged(AppUser? value)
    {
        if (value is null || !IsBrowsing) return;
    }

    private void ApplyUser(AppUser value)
    {
        Editor = new AppUser
        {
            Id = value.Id,
            UserName = value.UserName,
            DisplayName = value.DisplayName,
            IsManager = value.IsManager,
            PasswordHash = value.PasswordHash,
            EmployeeId = value.EmployeeId
        };
        NewPassword = "";
        foreach (var row in PermissionRows)
        {
            var p = value.Permissions.FirstOrDefault(x => x.AppScreenId == row.ScreenId);
            row.CanView = p?.CanView ?? false;
            row.CanCreate = p?.CanCreate ?? false;
            row.CanUpdate = p?.CanUpdate ?? false;
            row.CanDelete = p?.CanDelete ?? false;
            row.CanPrint = p?.CanPrint ?? false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            var perms = PermissionRows.Select(r => new UserPermission
            {
                AppScreenId = r.ScreenId,
                CanView = r.CanView,
                CanCreate = r.CanCreate,
                CanUpdate = r.CanUpdate,
                CanDelete = r.CanDelete,
                CanPrint = r.CanPrint
            }).ToList();
            await users.SaveAsync(Editor, string.IsNullOrWhiteSpace(NewPassword) ? null : NewPassword, perms);
            await LoadAsync();
        }, "Đã lưu người dùng.", closeEditor: true);
    }
}
