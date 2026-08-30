namespace Hma.Application.Services;

public sealed record DashboardSnapshot
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal FreightTotal { get; init; }
    public int PendingReconcile { get; init; }
    public int Reconciled { get; init; }
    public int WaitingDocuments { get; init; }
    public int TripCount { get; init; }
}
