namespace Hma.Desktop.Wpf.ViewModels;

/// <summary>
/// Một DbContext scoped cho cả phiên WPF — không được query/save chồng chéo.
/// </summary>
internal static class SessionDbGate
{
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private static readonly AsyncLocal<bool> Held = new();

    public static async Task RunAsync(Func<Task> action)
    {
        var owns = !Held.Value;
        if (owns)
        {
            await Lock.WaitAsync();
            Held.Value = true;
        }

        try
        {
            await action();
        }
        finally
        {
            if (owns)
            {
                Held.Value = false;
                Lock.Release();
            }
        }
    }

    public static async Task<T> RunAsync<T>(Func<Task<T>> action)
    {
        T result = default!;
        await RunAsync(async () => result = await action());
        return result;
    }
}
