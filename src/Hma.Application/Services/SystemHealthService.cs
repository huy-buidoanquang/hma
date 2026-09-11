using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class SystemHealthService(IHmaDbContext db, IFileStorage storage)
{
    public async Task<SystemHealthSnapshot> CheckAsync(CancellationToken ct = default)
    {
        var databaseAvailable = false;
        var documentStorageAvailable = false;
        var localFallback = true;
        var pendingExceptions = 0;
        var pendingReconciliations = 0;
        try
        {
            _ = await db.Parameters.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync(ct);
            pendingExceptions = await db.TransportExceptions.AsNoTracking().CountAsync(
                x => x.Status == TransportExceptionStatus.Draft
                     || x.Status == TransportExceptionStatus.Submitted, ct);
            pendingReconciliations = await db.DispatchOrders.AsNoTracking().CountAsync(
                x => x.Status == DispatchStatus.Completed
                     && x.ReconciliationStatus != ReconciliationStatus.Reconciled, ct);
            databaseAvailable = true;
        }
        catch
        {
            // Health result is returned to the operator instead of masking the storage check.
        }

        try
        {
            localFallback = await storage.IsLocalFallbackAsync(ct);
            await storage.CheckHealthAsync(ct);
            documentStorageAvailable = true;
        }
        catch
        {
            // The caller displays the failed component without leaking paths or credentials.
        }

        return new SystemHealthSnapshot
        {
            CheckedAt = DateTime.Now,
            DatabaseAvailable = databaseAvailable,
            DocumentStorageAvailable = documentStorageAvailable,
            UsesLocalDocumentStorage = localFallback,
            PendingTransportExceptions = pendingExceptions,
            PendingReconciliations = pendingReconciliations
        };
    }
}
