namespace Hma.Application.Features.Dispatching.Import;

public sealed class DispatchImportResult
{
    public int Saved { get; init; }
    public IReadOnlyList<DispatchImportCheck> Checks { get; init; } = [];
    public IReadOnlyList<DispatchImportRowError> Errors { get; init; } = [];
}
