using Hma.Desktop.Wpf.Abstractions;

namespace Hma.Desktop.Wpf.Infrastructure.Session;

public sealed class UiOperationGate : IUiOperationGate, IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly AsyncLocal<int> _depth = new();

    public async Task RunAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        var ownsLock = _depth.Value == 0;
        if (ownsLock)
            await _lock.WaitAsync(cancellationToken);

        _depth.Value++;
        try
        {
            await action();
        }
        finally
        {
            _depth.Value--;
            if (ownsLock)
                _lock.Release();
        }
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        T result = default!;
        await RunAsync(async () =>
        {
            result = await action();
        }, cancellationToken);
        return result;
    }

    public void Dispose() => _lock.Dispose();
}
