using Hma.Application.Features.Dispatching.Import;
namespace Hma.Application.Abstractions.Import;

public interface IDispatchImportParser
{
    IReadOnlyList<DispatchImportRow> Parse(Stream stream);
    void WriteTemplate(Stream stream);
    void WriteCheck(Stream sourceWorkbook, IReadOnlyList<DispatchImportCheck> rows, string? fileError, Stream output);
}
