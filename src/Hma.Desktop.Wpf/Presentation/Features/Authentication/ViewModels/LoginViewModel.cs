using Hma.Application.Abstractions.Security;
using Hma.Desktop.Wpf.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Hma.Desktop.Wpf.Presentation.Features.Authentication.ViewModels;

public partial class LoginViewModel(IAuthService auth, IDesktopUserSession session) : ObservableObject
{
    [ObservableProperty] private string userName = "";
    [ObservableProperty] private string? error;

    public event Action? LoggedIn;

    [RelayCommand]
    private async Task Login(string? password)
    {
        Error = null;
        var user = await auth.AuthenticateAsync(UserName.Trim(), password ?? "");
        if (user is null)
        {
            Error = "Sai tên đăng nhập hoặc mật khẩu.";
            return;
        }
        session.SignIn(user);
        LoggedIn?.Invoke();
    }
}
