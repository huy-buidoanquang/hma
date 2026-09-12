using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Common.Persistence;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Catalogs;

public sealed class JobTitleService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<CatalogItemSummary>> ListAsync(CancellationToken ct = default) =>
        db.JobTitles.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new CatalogItemSummary(x.Id, x.Code, x.Name)).ToListAsync(ct);

    public async Task SaveAsync(SaveCatalogItemCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.JobTitles, command.Id == 0);
        CatalogItemRules.EnsureCanSave(command.Code, command.Name, "chức vụ");
        if (await db.JobTitles.AnyAsync(x => x.Code == command.Code && x.Id != command.Id, ct))
            throw new InvalidOperationException("Mã chức vụ đã tồn tại.");
        var item = command.Id == 0
            ? new JobTitle()
            : await db.FindAsync<JobTitle>(command.Id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        item.Code = command.Code;
        item.Name = command.Name;
        if (item.Id == 0) db.Add(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.JobTitles, PermissionAction.Delete);
        var entity = await db.FindAsync<JobTitle>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, entity, ct);
    }
}
