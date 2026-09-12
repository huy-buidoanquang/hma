namespace Hma.Application.Abstractions.Persistence;

public interface IDocumentNumberService
{
    Task<string> NextAsync(string key, CancellationToken cancellationToken = default);
}
