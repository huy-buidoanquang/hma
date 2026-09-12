using Hma.Desktop.Wpf.Abstractions;
using Hma.Desktop.Wpf.Infrastructure.Session;
using NSubstitute;

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

    [Fact]
    public async Task Successful_crud_action_uses_shared_toast()
    {
        var toast = Substitute.For<IToastService>();
        var workspace = new ModeAwareWorkspace(toast);

        await workspace.SucceedAsync();

        toast.Received(1).Show("Đã lưu.", false);
    }

    [Fact]
    public async Task Business_rule_failure_uses_shared_error_toast()
    {
        var toast = Substitute.For<IToastService>();
        var workspace = new ModeAwareWorkspace(toast);

        await workspace.FailBusinessRuleAsync();

        toast.Received(1).Show("Dữ liệu không hợp lệ.", true);
    }

    private sealed class ModeAwareWorkspace : WorkspaceBase
    {
        public ModeAwareWorkspace() : this(Substitute.For<IToastService>())
        {
        }

        public ModeAwareWorkspace(IToastService toastService)
            : base(new UiOperationGate(), toastService)
        {
        }

        public bool CanPersist => Mode == WorkspaceMode.Create;
        public string Value { get; set; } = "initial";

        public void StartCreating() => EnterCreateState("Create", () => EditorState.Capture(Value));

        public Task SucceedAsync() => RunAsync(() => Task.CompletedTask, "Đã lưu.");

        public Task FailBusinessRuleAsync() => RunAsync(() =>
            Task.FromException(new InvalidOperationException("Dữ liệu không hợp lệ.")));

        protected override void OnWorkspaceModeChanged() =>
            OnPropertyChanged(nameof(CanPersist));
    }
}
