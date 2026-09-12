using Hma.Application.Abstractions.Security;
using Hma.Desktop.Wpf.Abstractions;
using Hma.Desktop.Wpf.Infrastructure.Dialogs;
using Hma.Desktop.Wpf.Infrastructure.Files;
using Hma.Desktop.Wpf.Infrastructure.Navigation;
using Hma.Desktop.Wpf.Infrastructure.Session;
using Hma.Desktop.Wpf.Infrastructure.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace Hma.Desktop.Wpf;

public static class DependencyInjection
{
    public static IServiceCollection AddHmaDesktop(this IServiceCollection services)
    {
        services.AddSingleton<DesktopUserSession>();
        services.AddSingleton<IDesktopUserSession>(provider => provider.GetRequiredService<DesktopUserSession>());
        services.AddSingleton<ICurrentUser>(provider => provider.GetRequiredService<DesktopUserSession>());
        services.AddSingleton<IDocumentInteractionService, DesktopDocumentInteractionService>();
        services.AddSingleton<IUiOperationGate, UiOperationGate>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<SessionHost>();
        services.AddSingleton<ISessionHost>(provider => provider.GetRequiredService<SessionHost>());
        services.AddSingleton<IUserPrompt, DialogUserPrompt>();

        services.AddTransient<LoginWindow>();
        services.AddTransient<LoginViewModel>();

        services.AddScoped<IWorkspaceNavigator, WorkspaceNavigator>();
        services.AddScoped<IWorkspaceRegistry, WorkspaceRegistry>();
        services.AddScoped<MainWindow>();
        services.AddScoped<MainViewModel>();
        services.AddScoped<CustomerWorkspaceViewModel>();
        services.AddScoped<PartnerWorkspaceViewModel>();
        services.AddScoped<DriverWorkspaceViewModel>();
        services.AddScoped<VehicleWorkspaceViewModel>();
        services.AddScoped<EmployeeWorkspaceViewModel>();
        services.AddScoped<DepartmentWorkspaceViewModel>();
        services.AddScoped<JobTitleWorkspaceViewModel>();
        services.AddScoped<CityWorkspaceViewModel>();
        services.AddScoped<LocationWorkspaceViewModel>();
        services.AddScoped<RouteWorkspaceViewModel>();
        services.AddScoped<PriceListWorkspaceViewModel>();
        services.AddScoped<PartnerRateWorkspaceViewModel>();
        services.AddScoped<PartnerSettlementWorkspaceViewModel>();
        services.AddScoped<TransportExceptionWorkspaceViewModel>();
        services.AddScoped<DispatchWorkspaceViewModel>();
        services.AddScoped<DispatchGridEditWorkspaceViewModel>();
        services.AddScoped<DispatchImportWorkspaceViewModel>();
        services.AddScoped<DispatchHubWorkspaceViewModel>();
        services.AddScoped<RouteCatalogWorkspaceViewModel>();
        services.AddScoped<ReconcileWorkspaceViewModel>();
        services.AddScoped<StatementWorkspaceViewModel>();
        services.AddScoped<LookupWorkspaceViewModel>();
        services.AddScoped<DashboardWorkspaceViewModel>();
        services.AddScoped<ReportWorkspaceViewModel>();
        services.AddScoped<SettingsWorkspaceViewModel>();
        services.AddScoped<LocationAliasWorkspaceViewModel>();
        services.AddScoped<RouteAliasWorkspaceViewModel>();
        services.AddScoped<CustomerAliasWorkspaceViewModel>();
        services.AddScoped<UserWorkspaceViewModel>();
        services.AddScoped<CashReceiptWorkspaceViewModel>();
        services.AddScoped<CashPaymentWorkspaceViewModel>();
        services.AddScoped<InvoiceWorkspaceViewModel>();
        return services;
    }
}
