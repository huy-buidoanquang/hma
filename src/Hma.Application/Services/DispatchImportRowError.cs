namespace Hma.Application.Services;

public sealed class DispatchImportRowError
{
    public int ExcelRow { get; init; }
    public string? SheetName { get; init; }
    public string? BlockLabel { get; init; }
    public string? Plate { get; init; }
    public string Message { get; init; } = "";
}
