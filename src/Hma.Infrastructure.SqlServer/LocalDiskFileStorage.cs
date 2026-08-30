using Hma.Application.Abstractions;
using Hma.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Infrastructure.SqlServer;

public sealed class LocalDiskFileStorage(IHmaDbContext db) : IFileStorage
{
    public async Task<bool> IsLocalFallbackAsync(CancellationToken cancellationToken = default)
    {
        var configured = await ConfiguredRootAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(configured);
    }

    public async Task<string> SaveAsync(string key, Stream content, CancellationToken cancellationToken = default)
    {
        var relative = FileStorageKey.Normalize(key);
        var dest = await CombineAsync(relative, cancellationToken);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        await using var output = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(output, cancellationToken);
        return relative;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = await ResolveExistingAsync(key, cancellationToken);
        if (path is null) return;
        try { File.Delete(path); } catch { /* ignore missing/locked files */ }
    }

    public async Task<string> GetPhysicalPathAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = await ResolveExistingAsync(key, cancellationToken)
                   ?? throw new InvalidOperationException("Không tìm thấy file chứng từ.");
        return path;
    }

    private async Task<string?> ResolveExistingAsync(string key, CancellationToken cancellationToken)
    {
        if (Path.IsPathRooted(key))
            return File.Exists(key) ? key : null;

        var dest = await CombineAsync(FileStorageKey.Normalize(key), cancellationToken);
        return File.Exists(dest) ? dest : null;
    }

    private async Task<string> CombineAsync(string relativeKey, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(await ResolveRootAsync(cancellationToken));
        var dest = Path.GetFullPath(Path.Combine(root, relativeKey.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!dest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(dest, root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đường dẫn chứng từ không hợp lệ.");
        return dest;
    }

    private async Task<string> ResolveRootAsync(CancellationToken cancellationToken)
    {
        var configured = await ConfiguredRootAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hma", "Documents");
    }

    private async Task<string?> ConfiguredRootAsync(CancellationToken cancellationToken)
    {
        var param = await db.Parameters.FirstOrDefaultAsync(p => p.Key == "DocumentStorePath", cancellationToken);
        return param?.Value?.Trim();
    }
}
