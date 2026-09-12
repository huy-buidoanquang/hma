using Hma.Desktop.Wpf.Abstractions;
using Hma.Application.Abstractions.Reporting;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Dispatching;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Catalogs.Models;
using Hma.Reporting;

namespace Hma.Desktop.Wpf.Presentation.Features.Catalogs.ViewModels;

public partial class DriverWorkspaceViewModel(
    DriverService driverService,
    CatalogOptionQueryService catalog,
    DispatchOrderQueryService dispatchQueries,
    ICurrentUser user,
    IUserPrompt prompt,
    IDocumentRenderer printer,
    IDocumentInteractionService documentInteraction, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private string? filterCode;
    [ObservableProperty] private string? filterName;
    [ObservableProperty] private DriverSummary? selected;
    [ObservableProperty] private DriverEditorModel editor = new();
    public ObservableCollection<DriverSummary> Items { get; } = [];
    public ObservableCollection<PartnerOption> Partners { get; } = [];
    public ObservableCollection<DispatchOrderSummary> Trips { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(
        Editor.Id, Editor.Code, Editor.Name, Editor.Phone, Editor.BirthDate, Editor.IdentityNumber, Editor.PartnerId);

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Drivers);
        UsePrompt(prompt);
        await RunAsync(async () =>
        {
            Partners.Clear();
            foreach (var p in await catalog.PartnersAsync()) Partners.Add(p);
            await Search();
        });
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var d in await driverService.SearchAsync(FilterCode, FilterName)) Items.Add(d);
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
        Editor = new DriverEditorModel();
        Trips.Clear();
        EnterCreateState("Thêm tài xế", CaptureEditorState);
    }

    [RelayCommand]
    private async Task ViewItem()
    {
        if (Selected is null) return;
        Editor = DriverEditorModel.From(Selected);
        Trips.Clear();
        foreach (var t in await dispatchQueries.TripsByDriverAsync(Editor.Id)) Trips.Add(t);
        EnterExistingState($"Xem tài xế — {Editor.Code}", $"Sửa tài xế — {Editor.Code}", CaptureEditorState);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await driverService.SaveAsync(Editor.ToCommand());
            await Search();
        }, "Đã lưu tài xế.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await driverService.DeleteAsync(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa.");
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        if (!CanPrint) return;
        await RunAsync(
            () => documentInteraction.OpenAsync(printer.ExportDriversExcel(Items.ToList())),
            "Đã xuất Excel tài xế.");
    }
}
