using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Common.Persistence;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Catalogs;

public sealed class DriverService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<DriverSummary>> SearchAsync(string? code = null, string? name = null, CancellationToken ct = default)
    {
        var query = db.Drivers.AsNoTracking().Include(x => x.Partner).AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) query = query.Where(x => x.Code.Contains(code));
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => x.Name.Contains(name));
        return query.OrderBy(x => x.Code).Take(500)
            .Select(x => new DriverSummary(x.Id, x.Code, x.Name, x.Phone, x.BirthDate, x.IdentityNumber, x.PartnerId,
                x.Partner == null ? null : new PartnerOption(
                    x.Partner.Id, x.Partner.Code, x.Partner.Name, x.Partner.OperatingFeePercent)))
            .ToListAsync(ct);
    }

    public async Task SaveAsync(SaveDriverCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Drivers, command.Id == 0);
        var item = command.Id == 0
            ? new Driver()
            : await db.FindAsync<Driver>(command.Id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        item.Code = command.Code;
        item.Name = command.Name;
        item.Phone = command.Phone;
        item.BirthDate = command.BirthDate;
        item.IdentityNumber = command.IdentityNumber;
        item.PartnerId = command.PartnerId;
        DriverRules.EnsureCanSave(item);
        if (await db.Drivers.AnyAsync(x => x.Code == item.Code && x.Id != item.Id, ct))
            throw new InvalidOperationException("Mã tài xế đã tồn tại.");
        if (!await db.Partners.AnyAsync(x => x.Id == item.PartnerId, ct))
            throw new InvalidOperationException("Đối tác không tồn tại.");
        if (item.Id == 0) db.Add(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Drivers, PermissionAction.Delete);
        var entity = await db.FindAsync<Driver>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, entity, ct);
    }
}
