using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class CustomerAliasService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<CustomerAlias>> ListAsync(CancellationToken ct = default) =>
        db.CustomerAliases.AsNoTracking()
            .Include(a => a.Customer)
            .OrderBy(a => a.Alias)
            .Take(500)
            .ToListAsync(ct);

    public async Task SaveAsync(CustomerAlias alias, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Settings, alias.Id == 0);
        alias.Alias = AliasText.Normalize(alias.Alias);
        CustomerAliasRules.EnsureCanSave(alias.Alias, alias.CustomerId);

        var others = await db.CustomerAliases.AsNoTracking()
            .Where(a => a.Id != alias.Id)
            .Select(a => a.Alias)
            .ToListAsync(ct);
        CustomerAliasRules.EnsureUnique(alias.Alias, others);

        var customers = await db.Customers.AsNoTracking().ToListAsync(ct);
        CustomerAliasRules.EnsureNotCustomerCatalog(alias.Alias, customers);
        if (!customers.Any(c => c.Id == alias.CustomerId))
            throw new InvalidOperationException("Khách hàng đích không tồn tại.");

        var customerId = alias.CustomerId;
        alias.Customer = null;
        alias.CustomerId = customerId;
        if (alias.Id == 0) db.Add(alias);
        else db.Update(alias);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Settings, PermissionAction.Delete);
        var entity = await db.FindAsync<CustomerAlias>(id, ct)
            ?? throw new InvalidOperationException("Không tìm thấy bí danh khách hàng.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, entity, ct);
    }
}
