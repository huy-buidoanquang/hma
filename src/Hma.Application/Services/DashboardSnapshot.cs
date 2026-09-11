namespace Hma.Application.Services;

public sealed record DashboardSnapshot
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal FreightTotal { get; init; }
    public decimal PartnerPayableTotal { get; init; }
    public decimal GrossMarginTotal { get; init; }
    public int PendingExceptions { get; init; }
    public int PendingReconcile { get; init; }
    public int Reconciled { get; init; }
    public int WaitingDocuments { get; init; }
    public int TripCount { get; init; }
    public int DraftCount { get; init; }
    public int IssuedCount { get; init; }
    public int CompletedCount { get; init; }
    public int CancelledCount { get; init; }
}
