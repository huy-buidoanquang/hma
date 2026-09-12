using System.Windows;
using Hma.Application;
using Hma.Desktop.Wpf.Infrastructure.Theming;
using Hma.Desktop.Wpf.Infrastructure.Session;
using Hma.Infrastructure.SqlServer;
using Hma.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hma.Desktop.Wpf;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private IServiceScope? _sessionScope;
    private ILogger<App>? _logger;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Login is the only window until MainWindow.Show(); default OnLastWindowClose
        // would shut the process down the moment DialogResult closes the login form.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        DispatcherUnhandledException += (_, args) =>
        {
            _logger?.LogError(args.Exception, "Unhandled WPF dispatcher exception");
            MessageBox.Show(args.Exception.Message, "Lỗi ứng dụng");
            args.Handled = true;
        };
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory);
                cfg.AddJsonFile("appsettings.json", optional: false);
            })
            // The default Windows Event Log provider can throw for standard user accounts
            // when its source is unavailable, masking the original startup exception.
            .ConfigureLogging(logging => logging.ClearProviders().AddDebug())
            .ConfigureServices((ctx, services) =>
            {
                var cs = Environment.GetEnvironmentVariable("HMA_CONNECTION");
                if (string.IsNullOrWhiteSpace(cs))
                    cs = ctx.Configuration.GetConnectionString("Hma");
                if (string.IsNullOrWhiteSpace(cs))
                    throw new InvalidOperationException(
                        "Thiếu chuỗi kết nối Hma. Đặt biến HMA_CONNECTION hoặc ConnectionStrings__Hma trên máy triển khai.");
                services.AddHmaApplication();
                services.AddHmaInfrastructure(cs);
                services.AddHmaReporting();
                services.AddHmaDesktop();
            })
            .Build();
        _logger = _host.Services.GetRequiredService<ILogger<App>>();

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
            _logger?.LogCritical(ex, "Database migration or seed failed during startup");
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
