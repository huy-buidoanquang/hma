using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hma.Infrastructure.SqlServer;

/// <summary>
/// Cho `dotnet ef migrations add` — không cần WPF làm startup project.
/// Chuỗi kết nối: biến môi trường HMA_CONNECTION, rồi appsettings.json gần thư mục làm việc.
/// </summary>
public sealed class HmaDbContextFactory : IDesignTimeDbContextFactory<HmaDbContext>
{
    public HmaDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();
        var options = new DbContextOptionsBuilder<HmaDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(HmaDbContext).Assembly.GetName().Name))
            .Options;
        return new HmaDbContext(options);
    }

    private static string ResolveConnectionString()
    {
        var fromEnv = Environment.GetEnvironmentVariable("HMA_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.Trim();

        foreach (var path in CandidateAppsettings())
        {
            if (!File.Exists(path))
                continue;
            var json = File.ReadAllText(path);
            var marker = "\"Hma\"";
            var markerAt = json.IndexOf(marker, StringComparison.Ordinal);
            if (markerAt < 0)
                continue;
            var colon = json.IndexOf(':', markerAt);
            var firstQuote = json.IndexOf('"', colon + 1);
            var secondQuote = json.IndexOf('"', firstQuote + 1);
            if (firstQuote < 0 || secondQuote < 0)
                continue;
            var value = json[(firstQuote + 1)..secondQuote];
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        throw new InvalidOperationException(
            "Thiếu chuỗi kết nối Hma. Đặt biến môi trường HMA_CONNECTION hoặc appsettings.json (ConnectionStrings:Hma).");
    }

    private static IEnumerable<string> CandidateAppsettings()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                var desktop = Path.Combine(dir.FullName, "src", "Hma.Desktop.Wpf", "appsettings.json");
                var nested = Path.Combine(dir.FullName, "Hma.Desktop.Wpf", "appsettings.json");
                var local = Path.Combine(dir.FullName, "appsettings.json");
                foreach (var path in new[] { desktop, nested, local })
                {
                    if (seen.Add(path))
                        yield return path;
                }
            }
        }
    }
}
