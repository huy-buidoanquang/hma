using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Common.Persistence;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Catalogs;

public sealed class PartnerService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<PartnerSummary>> SearchAsync(string? code = null, string? name = null, CancellationToken ct = default)
    {
        var query = db.Partners.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) query = query.Where(x => x.Code.Contains(code));
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => x.Name.Contains(name));
        return query.OrderBy(x => x.Code).Take(500)
            .Select(x => new PartnerSummary(x.Id, x.Code, x.Name, x.TaxCode, x.Address, x.ContactName,
                x.Phone, x.Email, x.OperatingFeePercent))
            .ToListAsync(ct);
    }

    public async Task SaveAsync(SavePartnerCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Partners, command.Id == 0);
        var item = command.Id == 0
            ? new Partner()
            : await db.FindAsync<Partner>(command.Id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        item.Code = command.Code;
        item.Name = command.Name;
        item.TaxCode = command.TaxCode;
        item.Address = command.Address;
        item.ContactName = command.ContactName;
        item.Phone = command.Phone;
        item.Email = command.Email;
        item.OperatingFeePercent = command.OperatingFeePercent;
        PartnerRules.EnsureCanSave(item);
        if (await db.Partners.AnyAsync(x => x.Code == item.Code && x.Id != item.Id, ct))
            throw new InvalidOperationException("Mã đối tác đã tồn tại.");
        if (item.Id == 0) db.Add(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Partners, PermissionAction.Delete);
        var entity = await db.FindAsync<Partner>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, entity, ct);
    }
}
