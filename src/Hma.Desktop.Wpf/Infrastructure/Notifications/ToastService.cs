using CommunityToolkit.Mvvm.ComponentModel;
using Hma.Desktop.Wpf.Abstractions;

namespace Hma.Desktop.Wpf.Infrastructure.Notifications;

public sealed partial class ToastService(TimeProvider timeProvider) : ObservableObject, IToastService
{
    private static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(4.5);

    [ObservableProperty] private string? message;
    [ObservableProperty] private bool isError;
    private int _generation;

    public void Show(string message, bool isError = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        IsError = isError;
        Message = message;
        var generation = Interlocked.Increment(ref _generation);
        _ = DismissAfterDelayAsync(generation);
    }

    public void Dismiss()
    {
        Interlocked.Increment(ref _generation);
        Message = null;
    }

    private async Task DismissAfterDelayAsync(int generation)
    {
        await Task.Delay(DisplayDuration, timeProvider);
        if (generation == Volatile.Read(ref _generation))
            Message = null;
    }
}
