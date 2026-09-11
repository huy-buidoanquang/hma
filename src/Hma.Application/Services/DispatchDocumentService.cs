using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class DispatchDocumentService(IHmaDbContext db, ICurrentUser current, IFileStorage files)
{
    public Task<bool> IsUsingLocalFallbackAsync(CancellationToken ct = default) =>
        files.IsLocalFallbackAsync(ct);

    public Task<List<DispatchDocument>> ListAsync(int dispatchOrderId, CancellationToken ct = default) =>
        db.DispatchDocuments.AsNoTracking().Where(d => d.DispatchOrderId == dispatchOrderId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);

    public async Task<DispatchDocument> AttachAsync(
        int dispatchOrderId,
        DispatchDocumentKind kind,
        string fileName,
        Stream content,
        CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Update);
        var order = await db.DispatchOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == dispatchOrderId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy lệnh điều xe.");
        DispatchWorkflowRules.EnsureCanMutateDocuments(order);

        var key = FileStorageKey.ForDispatchDocument(dispatchOrderId, fileName);
        var stored = await files.SaveAsync(key, content, ct);
        var doc = new DispatchDocument
        {
            DispatchOrderId = dispatchOrderId,
            Kind = kind,
            FileName = Path.GetFileName(fileName),
            StoredPath = stored,
            UploadedAt = DateTime.Now,
            UploadedByUserId = current.User?.Id
        };
        try
        {
            db.Add(doc);
            await PersistenceGuard.SaveAsync(db, ct);
        }
        catch
        {
            try { await files.DeleteAsync(stored, CancellationToken.None); }
            catch { /* Preserve the database error; storage cleanup is best-effort. */ }
            throw;
        }
        return doc;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Delete);
        var entity = await db.FindAsync<DispatchDocument>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy chứng từ.");
        var order = await db.DispatchOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == entity.DispatchOrderId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy lệnh điều xe.");
        DispatchWorkflowRules.EnsureCanMutateDocuments(order);
        db.Remove(entity);
        await PersistenceGuard.SaveAsync(db, ct);
        await files.DeleteAsync(entity.StoredPath, ct);
    }

    public Task<string> ResolvePhysicalPathAsync(string storedPath, CancellationToken ct = default) =>
        files.GetPhysicalPathAsync(storedPath, ct);
}
