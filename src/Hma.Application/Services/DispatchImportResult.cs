namespace Hma.Application.Services;

public sealed class DispatchImportResult
{
    public int Saved { get; init; }
    public IReadOnlyList<DispatchImportCheck> Checks { get; init; } = [];
    public IReadOnlyList<DispatchImportRowError> Errors { get; init; } = [];
}
