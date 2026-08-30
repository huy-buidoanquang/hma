using System.Text.Json;
using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class ChangeLogService(IHmaDbContext db, ICurrentUser current) : IChangeLogService
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
            UserId = current.User?.Id,
            ChangedAt = DateTime.Now
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<List<ChangeLog>> ForEntityAsync(string entityName, int entityId, CancellationToken ct = default) =>
        db.ChangeLogs.AsNoTracking().Include(c => c.User)
            .Where(c => c.EntityName == entityName && c.EntityId == entityId)
            .OrderByDescending(c => c.ChangedAt)
            .Take(100)
            .ToListAsync(ct);
}
