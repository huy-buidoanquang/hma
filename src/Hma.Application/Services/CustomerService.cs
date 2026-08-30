using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class CustomerService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<Customer>> SearchAsync(
        string? code, string? name, string? address, string? taxCode,
        DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var q = db.Customers.AsNoTracking().Include(c => c.City).Include(c => c.AccountantEmployee).AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) q = q.Where(c => c.Code.Contains(code));
        if (!string.IsNullOrWhiteSpace(name)) q = q.Where(c => c.Name.Contains(name));
        if (!string.IsNullOrWhiteSpace(address)) q = q.Where(c => c.Address != null && c.Address.Contains(address));
        if (!string.IsNullOrWhiteSpace(taxCode)) q = q.Where(c => c.TaxCode != null && c.TaxCode.Contains(taxCode));
        if (from is not null) q = q.Where(c => c.UpdatedAt >= from);
        if (to is not null) q = q.Where(c => c.UpdatedAt <= to.Value.Date.AddDays(1).AddTicks(-1));
        return q.OrderBy(c => c.Code).Take(500).ToListAsync(ct);
    }

    public Task<Customer?> GetAsync(int id, CancellationToken ct = default) =>
        db.Customers.Include(c => c.City).Include(c => c.AccountantEmployee).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Customer> SaveAsync(Customer customer, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Customers, customer.Id == 0);
        CustomerRules.EnsureCanSave(customer);

        var duplicate = await db.Customers.AnyAsync(c => c.Code == customer.Code && c.Id != customer.Id, ct);
        if (duplicate)
            throw new InvalidOperationException("Mã khách hàng này đã tồn tại.");

        customer.UpdatedAt = DateTime.Now;
        if (customer.Id == 0) db.Add(customer);
        else db.Update(customer);
        await db.SaveChangesAsync(ct);
        return customer;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Customers, PermissionAction.Delete);
        var entity = await db.FindAsync<Customer>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy khách hàng.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, ct);
    }
}
