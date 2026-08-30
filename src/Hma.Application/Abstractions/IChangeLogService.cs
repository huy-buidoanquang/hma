namespace Hma.Application.Abstractions;

public interface IChangeLogService
{
    Task RecordAsync(string entityName, int entityId, string action, string? summary, object? oldValue, object? newValue, CancellationToken cancellationToken = default);
}
