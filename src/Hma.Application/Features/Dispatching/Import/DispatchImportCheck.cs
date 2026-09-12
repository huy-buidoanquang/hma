namespace Hma.Application.Features.Dispatching.Import;

public sealed class DispatchImportCheck
{
    public required DispatchImportRow Row { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public bool IsValid => Errors.Count == 0;
}
