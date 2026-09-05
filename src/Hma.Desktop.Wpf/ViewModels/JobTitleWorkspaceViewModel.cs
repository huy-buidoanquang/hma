using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class JobTitleWorkspaceViewModel(CatalogService catalog, ICurrentUser user, IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private JobTitle? selected;
    [ObservableProperty] private JobTitle editor = new();
    public ObservableCollection<JobTitle> Items { get; } = [];

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.JobTitles);
        UsePrompt(prompt);
        Items.Clear();
        foreach (var j in await catalog.JobTitlesAsync()) Items.Add(j);
        Status = $"{Items.Count} chức vụ";
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Selected = null;
        Editor = new JobTitle();
        EnterCreate("Thêm chức vụ", Editor);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = new JobTitle { Id = Selected.Id, Code = Selected.Code, Name = Selected.Name };
        EnterExisting($"Xem chức vụ — {Editor.Code}", $"Sửa chức vụ — {Editor.Code}", Editor);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await catalog.SaveJobTitleAsync(Editor);
            await LoadAsync();
        }, "Đã lưu chức vụ.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await catalog.DeleteAsync<JobTitle>(Editor.Id);
            await LoadAsync();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }
}
