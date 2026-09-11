using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class TransportExceptionWorkspaceViewModel(
    TransportExceptionService exceptions,
    ICurrentUser current,
    IUserPrompt prompt) : WorkspaceBase
{
    [ObservableProperty] private TransportException editor = new();
    [ObservableProperty] private TransportException? selected;
    [ObservableProperty] private string? actionReason;

    public ObservableCollection<TransportException> Items { get; } = [];
    public ObservableCollection<TransportExceptionCode> Codes { get; } = [];
    public ObservableCollection<DispatchOrder> Orders { get; } = [];

    public bool CanPersist => CanSave
                              && Editor.Status is TransportExceptionStatus.Draft or TransportExceptionStatus.Rejected;
    public bool CanSubmit => CanUpdate && Editor.Id != 0
                             && Editor.Status is TransportExceptionStatus.Draft or TransportExceptionStatus.Rejected;
    public bool CanApprove => CanUpdate && current.User?.IsManager == true
                              && Editor.Status == TransportExceptionStatus.Submitted
                              && Editor.SubmittedByUserId != current.User?.Id;
    public bool CanReject => CanApprove;
    public bool CanVoid => CanUpdate && current.User?.IsManager == true
                           && Editor.Status == TransportExceptionStatus.Approved;
    public bool CanDeleteDraft => CanDelete && Editor.Id != 0
                                  && Editor.Status is TransportExceptionStatus.Draft
                                      or TransportExceptionStatus.Rejected;
    public bool IsNewException => Editor.Id == 0;
    public bool IsExistingException => !IsNewException;

    public override bool IsEditorReadOnly =>
        IsViewMode || Editor.Status is not TransportExceptionStatus.Draft and not TransportExceptionStatus.Rejected;

    public override async Task LoadAsync()
    {
        UsePermissions(current, ScreenKeys.TransportExceptions);
        UsePrompt(prompt);
        await ReloadLookupsAsync();
        await ReloadAsync();
    }

    private async Task ReloadLookupsAsync()
    {
        Codes.Clear();
        foreach (var code in await exceptions.CodesAsync()) Codes.Add(code);
        Orders.Clear();
        foreach (var order in await exceptions.EligibleOrdersAsync()) Orders.Add(order);
    }

    private async Task ReloadAsync()
    {
        Items.Clear();
        foreach (var item in await exceptions.ListAsync()) Items.Add(item);
        Status = $"{Items.Count} sự cố";
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Editor = new TransportException { OccurredAt = DateTime.Now };
        Selected = null;
        NotifyActions();
        EnterCreate("Thêm sự cố vận tải", Editor);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = Selected;
        ActionReason = Editor.ReviewNote ?? Editor.VoidReason;
        NotifyActions();
        EnterExisting($"Xem sự cố — {Editor.CodeSnapshot}", $"Sửa sự cố — {Editor.CodeSnapshot}", Editor);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanPersist) return;
        await RunAsync(async () =>
        {
            await exceptions.SaveAsync(Editor);
            await ReloadAsync();
        }, "Đã lưu sự cố vận tải.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Submit()
    {
        if (!CanSubmit) return;
        await RunActionAsync(() => exceptions.SubmitAsync(Editor.Id), "Đã gửi duyệt sự cố.");
    }

    [RelayCommand]
    private async Task DeleteDraft()
    {
        if (!CanDeleteDraft || !ConfirmDelete()) return;
        await RunActionAsync(() => exceptions.DeleteAsync(Editor.Id), "Đã xóa sự cố nháp.");
    }

    [RelayCommand]
    private async Task Approve()
    {
        if (!CanApprove) return;
        await RunActionAsync(() => exceptions.ApproveAsync(Editor.Id), "Đã duyệt và áp dụng chi phí sự cố.");
    }

    [RelayCommand]
    private async Task Reject()
    {
        if (!CanReject) return;
        await RunActionAsync(() => exceptions.RejectAsync(Editor.Id, ActionReason ?? ""), "Đã từ chối sự cố.");
    }

    [RelayCommand]
    private async Task Void()
    {
        if (!CanVoid) return;
        await RunActionAsync(() => exceptions.VoidAsync(Editor.Id, ActionReason ?? ""), "Đã hủy và hoàn chi phí sự cố.");
    }

    private async Task RunActionAsync(Func<Task> action, string success)
    {
        await RunAsync(async () =>
        {
            await action();
            await ReloadLookupsAsync();
            await ReloadAsync();
        }, success, closeEditor: true);
    }

    partial void OnEditorChanged(TransportException value) => NotifyActions();

    private void NotifyActions()
    {
        OnPropertyChanged(nameof(IsEditorReadOnly));
        OnPropertyChanged(nameof(AreFieldsEnabled));
        OnPropertyChanged(nameof(CanPersist));
        OnPropertyChanged(nameof(CanSubmit));
        OnPropertyChanged(nameof(CanApprove));
        OnPropertyChanged(nameof(CanReject));
        OnPropertyChanged(nameof(CanVoid));
        OnPropertyChanged(nameof(CanDeleteDraft));
        OnPropertyChanged(nameof(IsNewException));
        OnPropertyChanged(nameof(IsExistingException));
    }

    protected override void OnWorkspaceModeChanged() => NotifyActions();
}
