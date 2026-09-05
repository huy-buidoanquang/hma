using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Desktop.Wpf.Theming;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class SettingsWorkspaceViewModel(
    SettingsService settings,
    CompanyService company,
    LocationAliasWorkspaceViewModel locationAliases,
    RouteAliasWorkspaceViewModel routeAliases,
    CustomerAliasWorkspaceViewModel customerAliases,
    ICurrentUser user,
    IUserPrompt prompt,
    ThemeService theme) : WorkspaceBase
{
    [ObservableProperty] private string vatRate = "10";
    [ObservableProperty] private string documentStorePath = "";
    [ObservableProperty] private int dispatchOrderLastValue;
    [ObservableProperty] private int freightStatementLastValue;
    [ObservableProperty] private string uiTheme = ThemeService.Light;
    [ObservableProperty] private int companyId;
    [ObservableProperty] private string companyName = "";
    [ObservableProperty] private string? companyAddress;
    [ObservableProperty] private string? companyPhone;
    [ObservableProperty] private string? companyTaxCode;
    [ObservableProperty] private string? companyBank;
    [ObservableProperty] private string? companyWebsite;
    [ObservableProperty] private string? companyEmail;
    [ObservableProperty] private SettingsSection? selectedSection;
    private bool _loadingTheme;
    private bool _suppressSection;
    private string? _parametersBaseline;
    private string? _companyBaseline;

    public IReadOnlyList<ThemeOption> ThemeOptions => theme.Options;
    public LocationAliasWorkspaceViewModel LocationAliases { get; } = locationAliases;
    public RouteAliasWorkspaceViewModel RouteAliases { get; } = routeAliases;
    public CustomerAliasWorkspaceViewModel CustomerAliases { get; } = customerAliases;
    public IReadOnlyList<SettingsSection> Sections { get; } =
    [
        new() { Key = SettingsSectionKeys.Parameters, Title = "Tham số" },
        new() { Key = SettingsSectionKeys.Company, Title = "Công ty" },
        new() { Key = SettingsSectionKeys.LocationAliases, Title = "Từ điển điểm" },
        new() { Key = SettingsSectionKeys.RouteAliases, Title = "Từ điển tuyến" },
        new() { Key = SettingsSectionKeys.CustomerAliases, Title = "Từ điển khách" }
    ];

    public bool IsParametersSelected => SelectedSection?.Key == SettingsSectionKeys.Parameters;
    public bool IsCompanySelected => SelectedSection?.Key == SettingsSectionKeys.Company;
    public bool IsLocationAliasesSelected => SelectedSection?.Key == SettingsSectionKeys.LocationAliases;
    public bool IsRouteAliasesSelected => SelectedSection?.Key == SettingsSectionKeys.RouteAliases;
    public bool IsCustomerAliasesSelected => SelectedSection?.Key == SettingsSectionKeys.CustomerAliases;

    public override bool HasUnsavedChanges =>
        ParametersFingerprint != _parametersBaseline
        || CompanyFingerprint != _companyBaseline
        || LocationAliases.HasUnsavedChanges
        || RouteAliases.HasUnsavedChanges
        || CustomerAliases.HasUnsavedChanges;

    private string ParametersFingerprint =>
        EditorFingerprint.Of(VatRate, DocumentStorePath, DispatchOrderLastValue, FreightStatementLastValue);

    private string CompanyFingerprint =>
        EditorFingerprint.Of(CompanyId, CompanyName, CompanyAddress, CompanyPhone, CompanyTaxCode, CompanyBank, CompanyWebsite, CompanyEmail);

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Settings);
        UsePrompt(prompt);
        _loadingTheme = true;
        UiTheme = theme.Current;
        _loadingTheme = false;
        SelectedSection ??= Sections[0];
        await RunAsync(async () =>
        {
            var model = await settings.LoadAsync();
            VatRate = model.VatRate;
            DocumentStorePath = model.DocumentStorePath;
            DispatchOrderLastValue = model.DispatchOrderLastValue;
            FreightStatementLastValue = model.FreightStatementLastValue;
            _parametersBaseline = ParametersFingerprint;

            var info = await company.GetAsync();
            CompanyId = info.Id;
            CompanyName = info.Name;
            CompanyAddress = info.Address;
            CompanyPhone = info.Phone;
            CompanyTaxCode = info.TaxCode;
            CompanyBank = info.Bank;
            CompanyWebsite = info.Website;
            CompanyEmail = info.Email;
            _companyBaseline = CompanyFingerprint;

            await LocationAliases.LoadAsync();
            await RouteAliases.LoadAsync();
            await CustomerAliases.LoadAsync();
        });
    }

    partial void OnUiThemeChanged(string value)
    {
        if (_loadingTheme || string.IsNullOrWhiteSpace(value)) return;
        theme.Apply(value);
    }

    partial void OnSelectedSectionChanged(SettingsSection? oldValue, SettingsSection? newValue)
    {
        OnPropertyChanged(nameof(IsParametersSelected));
        OnPropertyChanged(nameof(IsCompanySelected));
        OnPropertyChanged(nameof(IsLocationAliasesSelected));
        OnPropertyChanged(nameof(IsRouteAliasesSelected));
        OnPropertyChanged(nameof(IsCustomerAliasesSelected));
        if (_suppressSection || oldValue is null || newValue is null || ReferenceEquals(oldValue, newValue))
            return;
        if (!HasUnsavedChanges)
            return;
        if (prompt.Confirm("Bỏ thay đổi chưa lưu?", "Xác nhận"))
        {
            _ = LoadAsync();
            return;
        }

        _suppressSection = true;
        SelectedSection = oldValue;
        _suppressSection = false;
    }

    [RelayCommand]
    private async Task SaveParameters()
    {
        if (!CanUpdate) return;
        await RunAsync(async () =>
        {
            await settings.SaveAsync(new SettingsModel
            {
                VatRate = VatRate,
                DocumentStorePath = DocumentStorePath,
                DispatchOrderLastValue = DispatchOrderLastValue,
                FreightStatementLastValue = FreightStatementLastValue
            });
            _parametersBaseline = ParametersFingerprint;
        }, "Đã lưu tham số.");
    }

    [RelayCommand]
    private async Task SaveCompany()
    {
        if (!CanUpdate) return;
        await RunAsync(async () =>
        {
            var row = new Company
            {
                Id = CompanyId,
                Name = CompanyName,
                Address = CompanyAddress,
                Phone = CompanyPhone,
                TaxCode = CompanyTaxCode,
                Bank = CompanyBank,
                Website = CompanyWebsite,
                Email = CompanyEmail
            };
            await company.SaveAsync(row);
            CompanyId = row.Id;
            _companyBaseline = CompanyFingerprint;
        }, "Đã lưu thông tin công ty.");
    }
}
