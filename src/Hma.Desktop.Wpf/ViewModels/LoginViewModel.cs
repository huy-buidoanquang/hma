using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hma.Application.Abstractions;

namespace Hma.Desktop.Wpf.ViewModels;

public partial class LoginViewModel(IAuthService auth) : ObservableObject
{
    [ObservableProperty] private string userName = "";
    [ObservableProperty] private string? error;

    public event Action? LoggedIn;

    [RelayCommand]
    private async Task Login(string? password)
    {
        Error = null;
        var user = await auth.LoginAsync(UserName.Trim(), password ?? "");
        if (user is null)
        {
            Error = "Sai tên đăng nhập hoặc mật khẩu.";
            return;
        }
        LoggedIn?.Invoke();
    }
}
