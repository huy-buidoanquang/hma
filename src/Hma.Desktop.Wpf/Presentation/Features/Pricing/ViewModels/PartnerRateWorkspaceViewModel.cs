using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Pricing;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Desktop.Wpf.Presentation.Features.Pricing.Models;

namespace Hma.Desktop.Wpf.Presentation.Features.Pricing.ViewModels;

public partial class PartnerRateWorkspaceViewModel(
    PartnerRateService rates,
    CatalogOptionQueryService catalog,
    ICurrentUser current,
    IUserPrompt prompt, IUiOperationGate operationGate) : WorkspaceBase(operationGate)
{
    [ObservableProperty] private PartnerRateEditorModel editor = new();
    [ObservableProperty] private PartnerRateSummary? selected;
    private byte[] _versionToken = [];

    public ObservableCollection<PartnerRateSummary> Items { get; } = [];
    public ObservableCollection<PartnerOption> Partners { get; } = [];
    public ObservableCollection<RouteOption> Routes { get; } = [];
    public ObservableCollection<VehicleTypeOption> VehicleTypes { get; } = [];

    private EditorState CaptureEditorState() => EditorState.Capture(
        Editor.Id, Editor.PartnerId, Editor.RouteId, Editor.VehicleTypeId,
        Editor.EffectiveFrom, Editor.EffectiveTo, Editor.UnitPrice, Editor.Surcharge);

    public override async Task LoadAsync()
    {
        UsePermissions(current, ScreenKeys.PartnerRates);
        UsePrompt(prompt);
        Partners.Clear();
        foreach (var partner in await catalog.PartnersAsync())
            if (partner.Code != "UNASSIGNED") Partners.Add(partner);
        Routes.Clear();
        foreach (var route in await catalog.RoutesAsync()) Routes.Add(route);
        VehicleTypes.Clear();
        foreach (var type in await catalog.VehicleTypesAsync()) VehicleTypes.Add(type);
        await Search();
    }

    [RelayCommand]
    private async Task Search()
    {
        Items.Clear();
        foreach (var rate in await rates.ListAsync()) Items.Add(rate);
        Status = $"{Items.Count} dòng giá mua";
    }

    [RelayCommand]
    private void NewItem()
    {
        if (!CanCreate) return;
        Editor = new PartnerRateEditorModel { EffectiveFrom = DateTime.Today };
        _versionToken = [];
        Selected = null;
        EnterCreateState("Thêm giá mua đối tác", CaptureEditorState);
    }

    [RelayCommand]
    private void ViewItem()
    {
        if (Selected is null) return;
        Editor = PartnerRateEditorModel.From(Selected);
        _versionToken = Selected.VersionToken.ToArray();
        EnterExistingState("Xem giá mua đối tác", "Sửa giá mua đối tác", CaptureEditorState);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave) return;
        await RunAsync(async () =>
        {
            await rates.SaveAsync(new SavePartnerRateCommand(
                Editor.Id, Editor.PartnerId, Editor.RouteId, Editor.VehicleTypeId,
                Editor.EffectiveFrom, Editor.EffectiveTo, Editor.UnitPrice, Editor.Surcharge, _versionToken));
            await Search();
        }, "Đã lưu giá mua đối tác.", closeEditor: true);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!CanDelete || Editor.Id == 0 || !ConfirmDelete()) return;
        await RunAsync(async () =>
        {
            await rates.DeleteAsync(Editor.Id);
            await Search();
            LeaveEditor(discardWithoutConfirm: true);
        }, "Đã xóa giá mua đối tác.");
    }
}
