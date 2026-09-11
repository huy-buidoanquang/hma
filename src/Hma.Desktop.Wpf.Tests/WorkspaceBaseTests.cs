using Hma.Desktop.Wpf.ViewModels;

namespace Hma.Desktop.Wpf.Tests;

public class WorkspaceBaseTests
{
    [Fact]
    public void Changing_mode_notifies_derived_action_state()
    {
        var workspace = new ModeAwareWorkspace();
        var changedProperties = new List<string?>();
        workspace.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        workspace.StartCreating();

        Assert.True(workspace.CanPersist);
        Assert.Contains(nameof(ModeAwareWorkspace.CanPersist), changedProperties);
    }

    private sealed class ModeAwareWorkspace : WorkspaceBase
    {
        public bool CanPersist => Mode == WorkspaceMode.Create;

        public void StartCreating() => EnterCreate("Create");

        protected override void OnWorkspaceModeChanged() =>
            OnPropertyChanged(nameof(CanPersist));
    }
}
