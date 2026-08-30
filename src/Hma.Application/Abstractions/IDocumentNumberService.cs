namespace Hma.Application.Abstractions;

public interface IDocumentNumberService
{
    Task<string> NextAsync(string key, CancellationToken cancellationToken = default);
}
