namespace Hma.Application.Features.Dispatching;

public sealed record DispatchDocumentDetails(
    int Id,
    int DispatchOrderId,
    DispatchDocumentKind Kind,
    string FileName,
    string StoredPath,
    DateTime UploadedAt,
    UserOption? UploadedByUser);
