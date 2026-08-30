using System.Windows;
using Hma.Desktop.Wpf.ViewModels;

namespace Hma.Desktop.Wpf.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConfirmDialogModel { IsDestructive: true })
            ConfirmButton.Style = (Style)FindResource("DestructiveButton");
        else
            ConfirmButton.Style = (Style)FindResource("PrimaryButton");
        ConfirmButton.Margin = new Thickness(0);
    }

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
