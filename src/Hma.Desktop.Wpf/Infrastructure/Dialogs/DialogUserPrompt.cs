using System.Windows;
using Hma.Desktop.Wpf.Abstractions;
using Hma.Desktop.Wpf.Presentation.Common.Models;
using Hma.Desktop.Wpf.Presentation.Common.Views;

namespace Hma.Desktop.Wpf.Infrastructure.Dialogs;

public sealed class DialogUserPrompt : IUserPrompt
{
    public bool Confirm(string message, string title, UserPromptKind kind = UserPromptKind.Question)
    {
        var destructive = kind == UserPromptKind.Destructive;
        var model = new ConfirmDialogModel(
            title,
            message,
            destructive ? "Xóa" : "Có",
            "Không",
            destructive);

        var dialog = new ConfirmDialog
        {
            DataContext = model,
            Owner = OwnerWindow()
        };
        if (dialog.Owner is null)
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        return dialog.ShowDialog() == true;
    }

    private static Window? OwnerWindow()
    {
        var current = System.Windows.Application.Current;
        if (current is null)
            return null;
        return current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
               ?? current.MainWindow;
    }
}
