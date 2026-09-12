using Hma.Application.Abstractions.Persistence;
using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Domain.Normalization;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Customers;

public class CustomerAliasService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<CustomerAliasSummary>> ListAsync(CancellationToken ct = default) =>
        db.CustomerAliases.AsNoTracking()
            .OrderBy(a => a.Alias)
            .Take(500)
            .Select(a => new CustomerAliasSummary(a.Id, a.Alias, a.CustomerId,
                a.Customer == null ? null : new CustomerOption(
                    a.Customer.Id, a.Customer.Code, a.Customer.Name, a.Customer.Address,
                    a.Customer.Phone, a.Customer.TaxCode, a.Customer.IsWalkIn)))
            .ToListAsync(ct);

    public async Task SaveAsync(SaveCustomerAliasCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Settings, command.Id == 0);
        var alias = command.Id == 0
            ? new CustomerAlias()
            : await db.FindAsync<CustomerAlias>(command.Id, ct) ?? throw new InvalidOperationException("Không tìm thấy bí danh khách hàng.");
        alias.Alias = command.Alias;
        alias.CustomerId = command.CustomerId;
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

        if (alias.Id == 0) db.Add(alias);
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
