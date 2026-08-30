using Hma.Application.Abstractions;
using Hma.Domain.Entities;
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
        db.Add(doc);
        await db.SaveChangesAsync(ct);
        return doc;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Delete);
        var entity = await db.FindAsync<DispatchDocument>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy chứng từ.");
        db.Remove(entity);
        await db.SaveChangesAsync(ct);
        await files.DeleteAsync(entity.StoredPath, ct);
    }

    public Task<string> ResolvePhysicalPathAsync(string storedPath, CancellationToken ct = default) =>
        files.GetPhysicalPathAsync(storedPath, ct);
}
