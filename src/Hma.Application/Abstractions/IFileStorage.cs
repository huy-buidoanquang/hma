namespace Hma.Application.Abstractions;

public interface IFileStorage
{
    Task<bool> IsLocalFallbackAsync(CancellationToken cancellationToken = default);
    Task CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<string> SaveAsync(string key, Stream content, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<string> GetPhysicalPathAsync(string key, CancellationToken cancellationToken = default);
}
