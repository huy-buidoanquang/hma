namespace Hma.Desktop.Wpf.Abstractions;

public interface IWorkspaceRegistry
{
    IReadOnlyList<WorkspaceEntry> Entries { get; }
}
