using Hma.Desktop.Wpf.Abstractions;
using Hma.Application.Abstractions.Reporting;
using System.IO;

namespace Hma.Desktop.Wpf.Abstractions;

public interface IDocumentInteractionService
{
    Task OpenAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
    Task OpenAsync(GeneratedDocument document, CancellationToken cancellationToken = default);
}
