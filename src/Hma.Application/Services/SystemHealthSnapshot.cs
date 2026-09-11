namespace Hma.Application.Services;

public sealed record SystemHealthSnapshot
{
    public DateTime CheckedAt { get; init; }
    public bool DatabaseAvailable { get; init; }
    public bool DocumentStorageAvailable { get; init; }
    public bool UsesLocalDocumentStorage { get; init; }
    public int PendingTransportExceptions { get; init; }
    public int PendingReconciliations { get; init; }

    public bool IsHealthy => DatabaseAvailable && DocumentStorageAvailable;
}
