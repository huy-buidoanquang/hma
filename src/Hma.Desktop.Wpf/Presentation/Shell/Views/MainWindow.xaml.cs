using System.Windows;

namespace Hma.Desktop.Wpf.Presentation.Shell.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
