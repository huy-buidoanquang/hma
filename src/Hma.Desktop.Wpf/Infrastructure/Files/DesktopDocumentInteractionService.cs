using System.Diagnostics;
using System.IO;
using Hma.Desktop.Wpf.Abstractions;
using Hma.Application.Abstractions.Reporting;

namespace Hma.Desktop.Wpf.Infrastructure.Files;

public sealed class DesktopDocumentInteractionService : IDocumentInteractionService
{
    public async Task OpenAsync(GeneratedDocument document, CancellationToken cancellationToken = default)
    {
        await using var content = new MemoryStream(document.Content, writable: false);
        await OpenAsync(document.FileName, content, cancellationToken);
    }

    public async Task OpenAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "document";
        var path = Path.Combine(Path.GetTempPath(), $"hma-{Guid.NewGuid():N}-{safeName}");
        await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        {
            await content.CopyToAsync(output, cancellationToken);
            await output.FlushAsync(cancellationToken);
        }

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
