using Hma.Application.Abstractions.Persistence;
using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Abstractions.Storage;
using Hma.Application.Common.Storage;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Dispatching;

public class DispatchDocumentService(
    IHmaDbContext db,
    ICurrentUser current,
    IFileStorage files,
    TimeProvider timeProvider)
{
    public Task<bool> IsUsingLocalFallbackAsync(CancellationToken ct = default) =>
        files.IsLocalFallbackAsync(ct);

    public async Task<List<DispatchDocumentDetails>> ListAsync(int dispatchOrderId, CancellationToken ct = default)
    {
        var rows = await db.DispatchDocuments.AsNoTracking().Include(d => d.UploadedByUser)
            .Where(d => d.DispatchOrderId == dispatchOrderId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);
        return rows.Select(ToDetails).ToList();
    }

    public async Task<DispatchDocumentDetails> AttachAsync(
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

        var now = timeProvider.GetLocalNow().DateTime;
        var key = FileStorageKey.ForDispatchDocument(dispatchOrderId, fileName, now);
        var stored = await files.SaveAsync(key, content, ct);
        var doc = new DispatchDocument
        {
            DispatchOrderId = dispatchOrderId,
            Kind = kind,
            FileName = Path.GetFileName(fileName),
            StoredPath = stored,
            UploadedAt = now,
            UploadedByUserId = current.UserId
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
        return ToDetails(doc);
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

    public Task<Stream> OpenReadAsync(string storedPath, CancellationToken ct = default) =>
        files.OpenReadAsync(storedPath, ct);

    private static DispatchDocumentDetails ToDetails(DispatchDocument item) => new(
        item.Id, item.DispatchOrderId, item.Kind, item.FileName, item.StoredPath, item.UploadedAt,
        item.UploadedByUser is null ? null : new UserOption(
            item.UploadedByUser.Id, item.UploadedByUser.UserName, item.UploadedByUser.DisplayName));
}
