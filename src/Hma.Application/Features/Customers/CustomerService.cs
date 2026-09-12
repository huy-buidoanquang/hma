using Hma.Application.Abstractions.Persistence;
using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Features.Catalogs;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Customers;

public class CustomerService(IHmaDbContext db, ICurrentUser current, TimeProvider timeProvider)
{
    public Task<List<CustomerSummary>> SearchAsync(
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
        return q.OrderBy(c => c.Code).Take(500)
            .Select(c => new CustomerSummary(
                c.Id, c.Code, c.Name, c.Address, c.Phone, c.TaxCode, c.ContactName, c.Email, c.CityId,
                c.City == null ? null : new CatalogOption(c.City.Id, c.City.Code, c.City.Name),
                c.AccountantEmployeeId,
                c.AccountantEmployee == null ? null : new EmployeeOption(
                    c.AccountantEmployee.Id, c.AccountantEmployee.Code, c.AccountantEmployee.Name,
                    c.AccountantEmployee.DepartmentId, null, c.AccountantEmployee.JobTitleId, null),
                c.IsWalkIn, c.UpdatedAt))
            .ToListAsync(ct);
    }

    internal Task<Customer?> GetAsync(int id, CancellationToken ct = default) =>
        db.Customers.Include(c => c.City).Include(c => c.AccountantEmployee).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<CustomerSummary> SaveAsync(SaveCustomerCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Customers, command.Id == 0);
        var customer = command.Id == 0
            ? new Customer()
            : await db.FindAsync<Customer>(command.Id, ct) ?? throw new InvalidOperationException("Không tìm thấy khách hàng.");
        customer.Code = command.Code;
        customer.Name = command.Name;
        customer.Address = command.Address;
        customer.Phone = command.Phone;
        customer.TaxCode = command.TaxCode;
        customer.ContactName = command.ContactName;
        customer.Email = command.Email;
        customer.CityId = command.CityId;
        customer.AccountantEmployeeId = command.AccountantEmployeeId;
        customer.IsWalkIn = command.IsWalkIn;
        CustomerRules.EnsureCanSave(customer);

        var duplicate = await db.Customers.AnyAsync(c => c.Code == customer.Code && c.Id != customer.Id, ct);
        if (duplicate)
            throw new InvalidOperationException("Mã khách hàng này đã tồn tại.");
        if (customer.CityId is int cityId
            && !await db.Cities.AnyAsync(c => c.Id == cityId, ct))
            throw new InvalidOperationException("Thành phố không tồn tại.");
        if (customer.AccountantEmployeeId is int accountantId
            && !await db.Employees.AnyAsync(e => e.Id == accountantId, ct))
            throw new InvalidOperationException("Nhân viên kế toán không tồn tại.");

        customer.UpdatedAt = timeProvider.GetLocalNow().DateTime;
        if (customer.Id == 0) db.Add(customer);
        await PersistenceGuard.SaveAsync(db, ct);
        return new CustomerSummary(
            customer.Id, customer.Code, customer.Name, customer.Address, customer.Phone, customer.TaxCode,
            customer.ContactName, customer.Email, customer.CityId, null, customer.AccountantEmployeeId, null,
            customer.IsWalkIn, customer.UpdatedAt);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Customers, PermissionAction.Delete);
        var entity = await db.FindAsync<Customer>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy khách hàng.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, entity, ct);
    }
}
