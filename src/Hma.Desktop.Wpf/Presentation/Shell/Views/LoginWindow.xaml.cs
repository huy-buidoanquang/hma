using System.Windows;
using System.Windows.Input;

namespace Hma.Desktop.Wpf.Presentation.Shell.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.LoggedIn += () =>
        {
            Dispatcher.Invoke(() =>
            {
                if (IsLoaded)
                    DialogResult = true;
            });
        };
        PasswordBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
                viewModel.LoginCommand.Execute(PasswordBox.Password);
        };
    }

    private void OnLoginClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.LoginCommand.Execute(PasswordBox.Password);
    }
}
