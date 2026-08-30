using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;
using Hma.Application.Services;
using Hma.Desktop.Wpf.Theming;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class SettingsWorkspaceViewModel(SettingsService settings, ICurrentUser user, ThemeService theme) : WorkspaceBase
{
    [ObservableProperty] private string vatRate = "10";
    [ObservableProperty] private string documentStorePath = "";
    [ObservableProperty] private int dispatchOrderLastValue;
    [ObservableProperty] private int freightStatementLastValue;
    [ObservableProperty] private string uiTheme = ThemeService.Light;
    private bool _loadingTheme;

    public IReadOnlyList<ThemeOption> ThemeOptions => theme.Options;

    public override async Task LoadAsync()
    {
        UsePermissions(user, ScreenKeys.Settings);
        _loadingTheme = true;
        UiTheme = theme.Current;
        _loadingTheme = false;
        await RunAsync(async () =>
        {
            var model = await settings.LoadAsync();
            VatRate = model.VatRate;
            DocumentStorePath = model.DocumentStorePath;
            DispatchOrderLastValue = model.DispatchOrderLastValue;
            FreightStatementLastValue = model.FreightStatementLastValue;
        });
    }

    partial void OnUiThemeChanged(string value)
    {
        if (_loadingTheme || string.IsNullOrWhiteSpace(value)) return;
        theme.Apply(value);
    }

    [RelayCommand]
    private async Task Save()
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
        }, "Đã lưu tham số.");
    }
}
