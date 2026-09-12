using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Common.Persistence;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Catalogs;

public sealed class CityService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<CatalogItemSummary>> ListAsync(CancellationToken ct = default) =>
        db.Cities.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new CatalogItemSummary(x.Id, x.Code, x.Name, x.Description)).ToListAsync(ct);

    public async Task SaveAsync(SaveCatalogItemCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Cities, command.Id == 0);
        CatalogItemRules.EnsureCanSave(command.Code, command.Name, "thành phố");
        if (await db.Cities.AnyAsync(x => x.Code == command.Code && x.Id != command.Id, ct))
            throw new InvalidOperationException("Mã thành phố đã tồn tại.");
        var city = command.Id == 0
            ? new City()
            : await db.FindAsync<City>(command.Id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        city.Code = command.Code;
        city.Name = command.Name;
        city.Description = command.Description;
        if (city.Id == 0) db.Add(city);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Cities, PermissionAction.Delete);
        var entity = await db.FindAsync<City>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, entity, ct);
    }
}
