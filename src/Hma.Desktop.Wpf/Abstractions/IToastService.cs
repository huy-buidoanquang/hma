using System.ComponentModel;

namespace Hma.Desktop.Wpf.Abstractions;

public interface IToastService : INotifyPropertyChanged
{
    string? Message { get; }
    bool IsError { get; }

    void Show(string message, bool isError = false);
    void Dismiss();
}
