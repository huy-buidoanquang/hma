namespace Hma.Desktop.Wpf.ViewModels;

public interface IUserPrompt
{
    bool Confirm(string message, string title, UserPromptKind kind = UserPromptKind.Question);
}
