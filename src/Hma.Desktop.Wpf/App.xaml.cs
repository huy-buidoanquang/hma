using System.Windows;
using Hma.Application;
using Hma.Application.Abstractions;
using Hma.Desktop.Wpf.Theming;
using Hma.Desktop.Wpf.ViewModels;
using Hma.Desktop.Wpf.Views;
using Hma.Infrastructure.SqlServer;
using Hma.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hma.Desktop.Wpf;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private IServiceScope? _sessionScope;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Login is the only window until MainWindow.Show(); default OnLastWindowClose
        // would shut the process down the moment DialogResult closes the login form.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "Lỗi ứng dụng");
            args.Handled = true;
        };
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory);
                cfg.AddJsonFile("appsettings.json", optional: false);
            })
            .ConfigureServices((ctx, services) =>
            {
                var cs = ctx.Configuration.GetConnectionString("Hma")
                         ?? throw new InvalidOperationException("Thiếu chuỗi kết nối Hma.");
                services.AddHmaApplication();
                services.AddHmaInfrastructure(cs);
                services.AddHmaReporting();
                services.AddSingleton<ThemeService>();
                services.AddSingleton<SessionHost>();
                services.AddSingleton<ISessionHost>(sp => sp.GetRequiredService<SessionHost>());
                services.AddSingleton<IWorkspaceNavigator, WorkspaceNavigator>();
                services.AddSingleton<IUserPrompt, DialogUserPrompt>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<LoginViewModel>();
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
                services.AddScoped<DispatchWorkspaceViewModel>();
                services.AddScoped<DispatchGridEditWorkspaceViewModel>();
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
            })
            .Build();

        await _host.StartAsync();
        _host.Services.GetRequiredService<ThemeService>().LoadAndApply();
        _host.Services.GetRequiredService<SessionHost>().SignOutHandler = SignOut;

        try
        {
            using var boot = _host.Services.CreateScope();
            var db = boot.ServiceProvider.GetRequiredService<HmaDatabaseInitializer>();
            await db.MigrateAndSeedAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Không kết nối được cơ sở dữ liệu");
            Shutdown();
            return;
        }

        if (e.Args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
        {
            Shutdown();
            return;
        }

        using (var loginScope = _host.Services.CreateScope())
        {
            var login = loginScope.ServiceProvider.GetRequiredService<LoginWindow>();
            if (login.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
        }

        if (!OpenMainWindow())
            Shutdown();
    }

    private void SignOut()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(SignOut);
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var previous = MainWindow;
        MainWindow = null;
        previous?.Close();
        _sessionScope?.Dispose();
        _sessionScope = null;
        if (_host?.Services.GetRequiredService<IWorkspaceNavigator>() is WorkspaceNavigator nav)
            nav.OpenDispatchHandler = null;

        using (var loginScope = _host!.Services.CreateScope())
        {
            var login = loginScope.ServiceProvider.GetRequiredService<LoginWindow>();
            if (login.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
        }

        if (!OpenMainWindow())
            Shutdown();
    }

    private bool OpenMainWindow()
    {
        try
        {
            _sessionScope = _host!.Services.CreateScope();
            var main = _sessionScope.ServiceProvider.GetRequiredService<MainWindow>();
            MainWindow = main;
            main.Show();
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Không mở được cửa sổ chính");
            return false;
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _sessionScope?.Dispose();
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
