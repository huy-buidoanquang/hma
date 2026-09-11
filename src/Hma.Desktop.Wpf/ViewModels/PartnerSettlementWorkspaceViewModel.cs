using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class PartnerSettlementWorkspaceViewModel(
    PartnerSettlementService settlements,
    CatalogService catalog,
    ICurrentUser current) : WorkspaceBase
{
    [ObservableProperty] private int? partnerId;
    [ObservableProperty] private int year = DateTime.Today.Year;
    [ObservableProperty] private int month = DateTime.Today.Month;
    [ObservableProperty] private PartnerSettlement? selected;
    [ObservableProperty] private PartnerSettlement? currentSettlement;
    [ObservableProperty] private string? voidReason;

    public ObservableCollection<Partner> Partners { get; } = [];
    public ObservableCollection<PartnerSettlement> Items { get; } = [];

    public bool CanGenerate => CanCreate && PartnerId is not null;
    public bool CanSubmit => CanUpdate && CurrentSettlement?.Status == FinancialDocumentStatus.Draft;
    public bool CanFinalize => CanUpdate
                               && CurrentSettlement?.Status == FinancialDocumentStatus.Submitted
                               && CurrentSettlement.SubmittedByUserId != current.User?.Id;
    public bool CanVoid => CanUpdate && current.User?.IsManager == true
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
        foreach (var item in await settlements.ListAsync()) Items.Add(item);
        NotifyActions();
    }

    [RelayCommand]
    private async Task Generate()
    {
        if (!CanGenerate || PartnerId is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrentSettlement(await settlements.GenerateAsync(PartnerId.Value, Year, Month));
            await ReloadAsync();
        }, "Đã lập quyết toán đối tác.");
    }

    [RelayCommand]
    private async Task OpenSelected()
    {
        if (Selected is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrentSettlement(await settlements.GetAsync(Selected.Id));
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
            RefreshCurrentSettlement(await settlements.SubmitAsync(CurrentSettlement.Id));
            await ReloadAsync();
        }, "Đã gửi duyệt quyết toán.");
    }

    [RelayCommand]
    private async Task Finalize()
    {
        if (!CanFinalize || CurrentSettlement is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrentSettlement(await settlements.FinalizeAsync(CurrentSettlement.Id));
            await ReloadAsync();
        }, "Đã chốt quyết toán đối tác.");
    }

    [RelayCommand]
    private async Task Void()
    {
        if (!CanVoid || CurrentSettlement is null) return;
        await RunAsync(async () =>
        {
            RefreshCurrentSettlement(await settlements.VoidAsync(CurrentSettlement.Id, VoidReason ?? ""));
            VoidReason = null;
            await ReloadAsync();
        }, "Đã hủy quyết toán đối tác.");
    }

    partial void OnPartnerIdChanged(int? value) => OnPropertyChanged(nameof(CanGenerate));
    partial void OnSelectedChanged(PartnerSettlement? value)
    {
        RefreshCurrentSettlement(value);
        if (value is not null)
            OpenSelectedCommand.Execute(null);
    }
    partial void OnCurrentSettlementChanged(PartnerSettlement? value) => NotifyActions();

    private void RefreshCurrentSettlement(PartnerSettlement? value)
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
