using Hma.Application.Features.Dispatching;

namespace Hma.Desktop.Wpf.Presentation.Features.Dispatching.Models;

public sealed record DispatchDocumentModel(
    int Id,
    int DispatchOrderId,
    DispatchDocumentKind Kind,
    string FileName,
    string StoredPath,
    DateTime UploadedAt,
    UserOption? UploadedByUser)
{
    public static DispatchDocumentModel From(DispatchDocumentDetails item) => new(
        item.Id, item.DispatchOrderId, item.Kind, item.FileName, item.StoredPath, item.UploadedAt,
        item.UploadedByUser);
}
