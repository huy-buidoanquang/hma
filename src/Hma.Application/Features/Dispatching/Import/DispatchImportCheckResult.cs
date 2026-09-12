namespace Hma.Application.Features.Dispatching.Import;

public sealed class DispatchImportCheckResult
{
    public string? FileError { get; init; }
    public IReadOnlyList<DispatchImportCheck> Rows { get; init; } = [];
    public int ValidCount { get; init; }
    public int ErrorCount { get; init; }
}
