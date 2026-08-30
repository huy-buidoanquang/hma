using System.Windows;
using Hma.Desktop.Wpf.ViewModels;

namespace Hma.Desktop.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
