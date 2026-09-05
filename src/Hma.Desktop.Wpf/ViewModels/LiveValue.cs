namespace Hma.Desktop.Wpf.ViewModels;

internal sealed class LiveValue
{
    private readonly Func<object?> _read;

    public LiveValue(Func<object?> read) => _read = read;

    public object? Value
    {
        get => _read();
        set { }
    }
}
