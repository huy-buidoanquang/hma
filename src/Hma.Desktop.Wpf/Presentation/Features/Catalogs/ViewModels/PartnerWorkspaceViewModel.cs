using Hma.Desktop.Wpf.Abstractions;
using Hma.Application.Abstractions.Reporting;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Catalogs.Models;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.Presentation.Features.Catalogs.ViewModels;

public partial class PartnerWorkspaceViewModel(
    PartnerService partnerService,
    ICurrentUser user,
    IUserPrompt prompt,
    IDocumentRenderer printer,
    IDocumentInteractionService documentInteraction, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private string? filterCode;
    [ObservableProperty] private string? filterName;
    [ObservableProperty] private PartnerSummary? selected;
    [ObservableProperty] private PartnerEditorModel editor = new();
    public ObservableCollection<PartnerSummary> Items { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(
        Editor.Id, Editor.Code, Editor.Name, Editor.TaxCode, Editor.Address,
        Editor.ContactName, Editor.Phone, Editor.Email, Editor.OperatingFeePercent);

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
        foreach (var p in await partnerService.SearchAsync(FilterCode, FilterName)) Items.Add(p);
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
        Editor = new PartnerEditorModel();
        EnterCreateState("Thêm đối tác", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = PartnerEditorModel.From(Selected);
        EnterExistingState($"Xem đối tác — {Editor.Code}", $"Sửa đối tác — {Editor.Code}", CaptureEditorState);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await partnerService.SaveAsync(Editor.ToCommand());
            await Search();
        }, "Đã lưu đối tác.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await partnerService.DeleteAsync(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        if (!CanPrint) return;
        await RunAsync(
            () => documentInteraction.OpenAsync(printer.ExportPartnersExcel(Items.ToList())),
            "Đã xuất Excel đối tác.");
    }
}
