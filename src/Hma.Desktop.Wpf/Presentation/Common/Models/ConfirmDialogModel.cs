namespace Hma.Desktop.Wpf.Presentation.Common.Models;

public sealed class ConfirmDialogModel
{
    public ConfirmDialogModel(string title, string message, string confirmText, string cancelText, bool isDestructive)
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
        IsDestructive = isDestructive;
    }

    public string Title { get; }
    public string Message { get; }
    public string ConfirmText { get; }
    public string CancelText { get; }
    public bool IsDestructive { get; }
}
