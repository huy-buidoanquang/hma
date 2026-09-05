namespace Hma.Application.Abstractions;

public interface IDispatchImportParser
{
    IReadOnlyList<Services.DispatchImportRow> Parse(Stream stream);
    void WriteTemplate(Stream stream);
    void WriteCheck(Stream sourceWorkbook, IReadOnlyList<Services.DispatchImportCheck> rows, string? fileError, Stream output);
}
