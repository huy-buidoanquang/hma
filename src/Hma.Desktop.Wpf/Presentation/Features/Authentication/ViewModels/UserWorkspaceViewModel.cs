using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Common.Authorization;
using Hma.Application.Features.Authentication;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Authentication.Models;

namespace Hma.Desktop.Wpf.Presentation.Features.Authentication.ViewModels;

public partial class UserWorkspaceViewModel(UserAdminService users, CatalogOptionQueryService catalog, ICurrentUser user, IUserPrompt prompt, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private UserEditorModel editor = new();
    [ObservableProperty] private UserSummary? selected;
    [ObservableProperty] private string newPassword = "";
    public ObservableCollection<UserSummary> Items { get; } = [];
    public ObservableCollection<PermissionRow> PermissionRows { get; } = [];
    public ObservableCollection<EmployeeOption> Employees { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(
        Editor.Id, Editor.UserName, Editor.DisplayName, Editor.EmployeeId, Editor.IsManager,
        Editor.IsSpecial, NewPassword,
        string.Join(";", PermissionRows.Select(row =>
            $"{row.ScreenId},{row.CanCreate},{row.CanDelete},{row.CanUpdate},{row.CanView},{row.CanPrint}")));

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
        Editor = new UserEditorModel();
        NewPassword = "";
        foreach (var row in PermissionRows)
            row.CanView = row.CanCreate = row.CanUpdate = row.CanDelete = row.CanPrint = false;
        EnterCreateState("Thêm người dùng", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        ApplyUser(Selected);
        EnterExistingState($"Xem người dùng — {Editor.UserName}", $"Sửa người dùng — {Editor.UserName}",
            CaptureEditorState);
    }

    partial void OnSelectedChanged(UserSummary? value)
    {
        if (value is null || !IsBrowsing) return;
    }

    private void ApplyUser(UserSummary value)
    {
        Editor = UserEditorModel.From(value);
        NewPassword = "";
        foreach (var row in PermissionRows)
        {
            var p = value.Permissions.FirstOrDefault(x => x.ScreenId == row.ScreenId)?.Grant;
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
            var permissions = PermissionRows.Select(r => new UserPermissionDetails(r.ScreenId,
                new PermissionGrant(r.CanCreate, r.CanDelete, r.CanUpdate, r.CanView, r.CanPrint))).ToList();
            await users.SaveAsync(new SaveUserCommand(
                Editor.Id, Editor.UserName, Editor.DisplayName, Editor.EmployeeId, Editor.IsManager,
                Editor.IsSpecial, string.IsNullOrWhiteSpace(NewPassword) ? null : NewPassword, permissions));
            await LoadAsync();
        }, "Đã lưu người dùng.", closeEditor: true);
    }
}
