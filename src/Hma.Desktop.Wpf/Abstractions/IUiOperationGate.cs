namespace Hma.Desktop.Wpf.Abstractions;

public interface IUiOperationGate
{
    Task RunAsync(Func<Task> action, CancellationToken cancellationToken = default);

    Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
}
