using Hma.Application.Features.Dispatching;

namespace Hma.Application.Features.Reconciliation;

public sealed record AuditEntrySummary(
    int Id, string Action, string? Summary, DateTime ChangedAt, UserOption? User);
