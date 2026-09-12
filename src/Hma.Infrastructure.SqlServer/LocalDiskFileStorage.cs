using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Storage;
using Hma.Application.Common.Storage;
using Microsoft.EntityFrameworkCore;

namespace Hma.Infrastructure.SqlServer;

public sealed class LocalDiskFileStorage(IHmaDbContext db) : IFileStorage
{
    public async Task<bool> IsLocalFallbackAsync(CancellationToken cancellationToken = default)
    {
        var configured = await ConfiguredRootAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(configured);
    }

    public async Task CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(await ResolveRootAsync(cancellationToken));
        Directory.CreateDirectory(root);
        var probe = Path.Combine(root, $".hma-health-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(probe, "ok", cancellationToken);
            await using var stream = new FileStream(probe, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length != 2)
                throw new IOException("Không thể xác nhận kho chứng từ.");
        }
        finally
        {
            if (File.Exists(probe))
                File.Delete(probe);
        }
    }

    public async Task<string> SaveAsync(string key, Stream content, CancellationToken cancellationToken = default)
    {
        var relative = FileStorageKey.Normalize(key);
        var dest = await CombineAsync(relative, cancellationToken);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        var temporary = $"{dest}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await content.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
            }
            File.Move(temporary, dest, overwrite: false);
        }
        catch
        {
            try { File.Delete(temporary); }
            catch { /* Preserve the original storage error. */ }
            throw;
        }
        return relative;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = await ResolveExistingAsync(key, cancellationToken);
        if (path is null) return;
        try { File.Delete(path); } catch { /* ignore missing/locked files */ }
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = await ResolveExistingAsync(key, cancellationToken)
                   ?? throw new InvalidOperationException("Không tìm thấy file chứng từ.");
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
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
