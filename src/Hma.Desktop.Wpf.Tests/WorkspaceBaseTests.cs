using Hma.Desktop.Wpf.Infrastructure.Session;

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

    [Fact]
    public void Explicit_editor_state_detects_a_changed_value()
    {
        var workspace = new ModeAwareWorkspace();
        workspace.StartCreating();

        workspace.Value = "changed";

        Assert.True(workspace.HasUnsavedChanges);
    }

    private sealed class ModeAwareWorkspace() : WorkspaceBase(new UiOperationGate())
    {
        public bool CanPersist => Mode == WorkspaceMode.Create;
        public string Value { get; set; } = "initial";

        public void StartCreating() => EnterCreateState("Create", () => EditorState.Capture(Value));

        protected override void OnWorkspaceModeChanged() =>
            OnPropertyChanged(nameof(CanPersist));
    }
}
