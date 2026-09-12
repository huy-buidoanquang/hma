namespace Hma.Desktop.Wpf.Abstractions;

public interface IUserPrompt
{
    bool Confirm(string message, string title, UserPromptKind kind = UserPromptKind.Question);
}
