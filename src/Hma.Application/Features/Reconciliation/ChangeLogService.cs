using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Abstractions.Persistence;
using System.Text.Json;
using Hma.Domain.Entities;
using Hma.Application.Features.Dispatching;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Reconciliation;

public class ChangeLogService(IHmaDbContext db, ICurrentUser current, TimeProvider timeProvider) : IChangeLogService
{
    public async Task RecordAsync(string entityName, int entityId, string action, string? summary, object? oldValue, object? newValue, CancellationToken cancellationToken = default)
    {
        db.Add(new ChangeLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            Summary = summary,
            OldJson = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
            NewJson = newValue is null ? null : JsonSerializer.Serialize(newValue),
            UserId = current.UserId,
            ChangedAt = timeProvider.GetLocalNow().DateTime
        });
        await PersistenceGuard.SaveAsync(db, cancellationToken);
    }

    public Task<List<AuditEntrySummary>> ForEntityAsync(string entityName, int entityId, CancellationToken ct = default) =>
        db.ChangeLogs.AsNoTracking().Include(c => c.User)
            .Where(c => c.EntityName == entityName && c.EntityId == entityId)
            .OrderByDescending(c => c.ChangedAt)
            .Take(100)
            .Select(c => new AuditEntrySummary(c.Id, c.Action, c.Summary, c.ChangedAt,
                c.User == null ? null : new UserOption(c.User.Id, c.User.UserName, c.User.DisplayName)))
            .ToListAsync(ct);
}
