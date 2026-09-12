using Hma.Application.Features.PartnerSettlements;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Catalogs;
using Hma.Application.Common.Authorization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.Presentation.Features.PartnerSettlements.ViewModels;

public partial class PartnerSettlementWorkspaceViewModel(
    PartnerSettlementService settlements,
    CatalogOptionQueryService catalog,
    ICurrentUser current, IUiOperationGate operationGate, IToastService toastService) : WorkspaceBase(operationGate, toastService)
{
    [ObservableProperty] private int? partnerId;
    [ObservableProperty] private int year = DateTime.Today.Year;
    [ObservableProperty] private int month = DateTime.Today.Month;
    [ObservableProperty] private PartnerSettlementDetails? selected;
    [ObservableProperty] private PartnerSettlementDetails? currentSettlement;
    [ObservableProperty] private string? voidReason;

    public ObservableCollection<PartnerOption> Partners { get; } = [];
    public ObservableCollection<PartnerSettlementDetails> Items { get; } = [];

    public bool CanGenerate => CanCreate && PartnerId is not null;
    public bool CanSubmit => CanUpdate && CurrentSettlement?.Status == FinancialDocumentStatus.Draft;
    public bool CanFinalize => CanUpdate
                               && CurrentSettlement?.Status == FinancialDocumentStatus.Submitted
                               && CurrentSettlement.SubmittedByUserId != current.UserId;
    public bool CanVoid => CanUpdate && current.IsManager
                           && CurrentSettlement is not null
                           && CurrentSettlement.Status != FinancialDocumentStatus.Voided;

    public override async Task LoadAsync()
    {
        UsePermissions(current, ScreenKeys.PartnerSettlements);
        Partners.Clear();
        foreach (var partner in await catalog.PartnersAsync())
            if (partner.Code != "UNASSIGNED") Partners.Add(partner);
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        Items.Clear();
        foreach (var item in await settlements.ListDetailsAsync()) Items.Add(item);
        NotifyActions();
    }

    [RelayCommand]
    private async Task Generate()
    {
        if (!CanGenerate || PartnerId is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrentSettlement(await settlements.GenerateDetailsAsync(PartnerId.Value, Year, Month));
            await ReloadAsync();
        }, "Đã lập quyết toán đối tác.");
    }

    [RelayCommand]
    private async Task OpenSelected()
    {
        if (Selected is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrentSettlement(await settlements.GetDetailsAsync(Selected.Id));
            if (CurrentSettlement is not null)
            {
                PartnerId = CurrentSettlement.PartnerId;
                Year = CurrentSettlement.Year;
                Month = CurrentSettlement.Month;
            }
            NotifyActions();
        });
    }

    [RelayCommand]
    private async Task Submit()
    {
        if (!CanSubmit || CurrentSettlement is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrentSettlement(await settlements.SubmitDetailsAsync(CurrentSettlement.Id));
            await ReloadAsync();
        }, "Đã gửi duyệt quyết toán.");
    }

    [RelayCommand]
    private async Task Finalize()
    {
        if (!CanFinalize || CurrentSettlement is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrentSettlement(await settlements.FinalizeDetailsAsync(CurrentSettlement.Id));
            await ReloadAsync();
        }, "Đã chốt quyết toán đối tác.");
    }

    [RelayCommand]
    private async Task Void()
    {
        if (!CanVoid || CurrentSettlement is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrentSettlement(await settlements.VoidDetailsAsync(CurrentSettlement.Id, VoidReason ?? ""));
            VoidReason = null;
            await ReloadAsync();
        }, "Đã hủy quyết toán đối tác.");
    }

    partial void OnPartnerIdChanged(int? value) => OnPropertyChanged(nameof(CanGenerate));
    partial void OnSelectedChanged(PartnerSettlementDetails? value)
    {
        RefreshCurrentSettlement(value);
        if (value is not null)
            OpenSelectedCommand.Execute(null);
    }
    partial void OnCurrentSettlementChanged(PartnerSettlementDetails? value) => NotifyActions();

    private void RefreshCurrentSettlement(PartnerSettlementDetails? value)
    {
        CurrentSettlement = value;
        OnPropertyChanged(nameof(CurrentSettlement));
        NotifyActions();
    }

    private void NotifyActions()
    {
        OnPropertyChanged(nameof(CanGenerate));
        OnPropertyChanged(nameof(CanSubmit));
        OnPropertyChanged(nameof(CanFinalize));
        OnPropertyChanged(nameof(CanVoid));
    }
}
