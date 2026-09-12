using Hma.Application.Abstractions.Security;
using Hma.Desktop.Wpf.Abstractions;
using Hma.Desktop.Wpf.Presentation.Shell.ViewModels;
using NSubstitute;

namespace Hma.Desktop.Wpf.Tests;

public class WorkspaceRegistryTests
{
    [Fact]
    public void Main_shell_uses_registry_order_and_selects_first_workspace()
    {
        var firstWorkspace = new object();
        var entries = new[]
        {
            new WorkspaceEntry("first", "Đầu tiên", firstWorkspace),
            new WorkspaceEntry("second", "Thứ hai", new object()),
        };
        var registry = Substitute.For<IWorkspaceRegistry>();
        registry.Entries.Returns(entries);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.DisplayName.Returns("Điều hành viên");
        var toast = Substitute.For<IToastService>();

        var viewModel = new MainViewModel(
            currentUser,
            Substitute.For<IDesktopUserSession>(),
            Substitute.For<IUserPrompt>(),
            Substitute.For<ISessionHost>(),
            Substitute.For<IWorkspaceNavigator>(),
            registry,
            toast);

        Assert.Equal(entries, viewModel.Items);
        Assert.Same(entries[0], viewModel.Selected);
        Assert.Same(firstWorkspace, viewModel.Current);
        Assert.Equal("Điều hành viên", viewModel.UserLabel);
        Assert.Same(toast, viewModel.Toast);
    }
}
