using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Desktop.Wpf.Theming;
using Hma.Domain.Entities;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class SettingsWorkspaceViewModel(
    SettingsService settings,
    SystemHealthService health,
    CompanyService company,
    CityWorkspaceViewModel cities,
    UserWorkspaceViewModel users,
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
    [ObservableProperty] private string healthStatus = "Chưa kiểm tra";
    [ObservableProperty] private SettingsSection? selectedSection;
    private bool _loadingTheme;
    private bool _suppressSection;
    private string? _parametersBaseline;
    private string? _companyBaseline;

    public IReadOnlyList<ThemeOption> ThemeOptions => theme.Options;
    public CityWorkspaceViewModel Cities { get; } = cities;
    public UserWorkspaceViewModel Users { get; } = users;
    public LocationAliasWorkspaceViewModel LocationAliases { get; } = locationAliases;
    public RouteAliasWorkspaceViewModel RouteAliases { get; } = routeAliases;
    public CustomerAliasWorkspaceViewModel CustomerAliases { get; } = customerAliases;
    public IReadOnlyList<SettingsSection> Sections { get; } = BuildSections(user);

    public bool IsParametersSelected => SelectedSection?.Key == SettingsSectionKeys.Parameters;
    public bool IsCompanySelected => SelectedSection?.Key == SettingsSectionKeys.Company;
    public bool IsCitiesSelected => SelectedSection?.Key == SettingsSectionKeys.Cities;
    public bool IsUsersSelected => SelectedSection?.Key == SettingsSectionKeys.Users;
    public bool IsLocationAliasesSelected => SelectedSection?.Key == SettingsSectionKeys.LocationAliases;
    public bool IsRouteAliasesSelected => SelectedSection?.Key == SettingsSectionKeys.RouteAliases;
    public bool IsCustomerAliasesSelected => SelectedSection?.Key == SettingsSectionKeys.CustomerAliases;

    public override bool HasUnsavedChanges =>
        ParametersFingerprint != _parametersBaseline
        || CompanyFingerprint != _companyBaseline
        || Cities.HasUnsavedChanges
        || Users.HasUnsavedChanges
        || LocationAliases.HasUnsavedChanges
        || RouteAliases.HasUnsavedChanges
        || CustomerAliases.HasUnsavedChanges;

    private string ParametersFingerprint =>
        EditorFingerprint.Of(VatRate, DocumentStorePath, DispatchOrderLastValue, FreightStatementLastValue);

    private string CompanyFingerprint =>
        EditorFingerprint.Of(CompanyId, CompanyName, CompanyAddress, CompanyPhone, CompanyTaxCode, CompanyBank, CompanyWebsite, CompanyEmail);

    private bool CanLoadSettingsBody =>
        user.User?.IsManager == true || user.Can(ScreenKeys.Settings, PermissionAction.View);

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Settings);
        UsePrompt(prompt);
        Cities.DiscardUnsavedEditor();
        Users.DiscardUnsavedEditor();
        LocationAliases.DiscardUnsavedEditor();
        RouteAliases.DiscardUnsavedEditor();
        CustomerAliases.DiscardUnsavedEditor();
        _loadingTheme = true;
        UiTheme = theme.Current;
        _loadingTheme = false;
        if (SelectedSection is null && Sections.Count > 0)
            SelectedSection = Sections[0];
        await RunAsync(async () =>
        {
            if (CanLoadSettingsBody)
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
            }
            else
            {
                _parametersBaseline = ParametersFingerprint;
                _companyBaseline = CompanyFingerprint;
            }

            if (Sections.Any(s => s.Key == SettingsSectionKeys.Cities))
                await Cities.LoadAsync();
            if (Sections.Any(s => s.Key == SettingsSectionKeys.Users))
                await Users.LoadAsync();
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
        OnPropertyChanged(nameof(IsCitiesSelected));
        OnPropertyChanged(nameof(IsUsersSelected));
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

    [RelayCommand]
    private async Task CheckHealth()
    {
        await RunAsync(async () =>
        {
            var result = await health.CheckAsync();
            var database = result.DatabaseAvailable ? "SQL: tốt" : "SQL: lỗi";
            var storage = result.DocumentStorageAvailable ? "Kho chứng từ: tốt" : "Kho chứng từ: lỗi";
            var topology = result.UsesLocalDocumentStorage ? "đang dùng thư mục cục bộ" : "đang dùng thư mục dùng chung";
            HealthStatus = $"{result.CheckedAt:dd/MM/yyyy HH:mm} · {database} · {storage} · {topology} · "
                           + $"{result.PendingTransportExceptions} sự cố chờ xử lý · {result.PendingReconciliations} chuyến chờ đối soát";
        });
    }

    private static IReadOnlyList<SettingsSection> BuildSections(ICurrentUser current)
    {
        var manager = current.User?.IsManager == true;
        var settingsView = manager || current.Can(ScreenKeys.Settings, PermissionAction.View);
        var citiesView = manager || current.Can(ScreenKeys.Cities, PermissionAction.View)
                         || current.Can(ScreenKeys.Cities, PermissionAction.Create);
        var list = new List<SettingsSection>();
        if (settingsView)
        {
            list.Add(new() { Key = SettingsSectionKeys.Parameters, Title = "Tham số" });
            list.Add(new() { Key = SettingsSectionKeys.Company, Title = "Công ty" });
        }

        if (citiesView)
            list.Add(new() { Key = SettingsSectionKeys.Cities, Title = "Thành phố" });
        if (manager)
            list.Add(new() { Key = SettingsSectionKeys.Users, Title = "Người dùng" });
        if (settingsView)
        {
            list.Add(new() { Key = SettingsSectionKeys.LocationAliases, Title = "Từ điển điểm" });
            list.Add(new() { Key = SettingsSectionKeys.RouteAliases, Title = "Từ điển tuyến" });
            list.Add(new() { Key = SettingsSectionKeys.CustomerAliases, Title = "Từ điển khách" });
        }

        return list;
    }
}
