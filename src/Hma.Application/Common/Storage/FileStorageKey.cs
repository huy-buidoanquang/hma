using System.Text;

namespace Hma.Application.Common.Storage;

public static class FileStorageKey
{
    public static string ForDispatchDocument(int dispatchOrderId, string fileName, DateTime timestamp)
    {
        var safe = SanitizeFileName(fileName);
        return $"{dispatchOrderId:000000}/{timestamp:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}-{safe}";
    }

    public static string Normalize(string key)
    {
        var trimmed = key.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Đường dẫn chứng từ không hợp lệ.");
        var parts = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Any(p => p.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            throw new InvalidOperationException("Đường dẫn chứng từ không hợp lệ.");
        return string.Join('/', parts);
    }

    public static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Tên file chứng từ không hợp lệ.");
        var invalid = Path.GetInvalidFileNameChars();
        var buffer = new StringBuilder(name.Length);
        foreach (var ch in name)
            buffer.Append(invalid.Contains(ch) ? '_' : ch);
        return buffer.ToString();
    }
}
