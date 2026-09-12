using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Hma.Application.Common.Persistence;
using Hma.Domain.Entities;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Catalogs;

public sealed class EmployeeService(IHmaDbContext db, ICurrentUser current, TimeProvider timeProvider)
{
    public Task<List<EmployeeSummary>> ListAsync(CancellationToken ct = default) =>
        db.Employees.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new EmployeeSummary(
                x.Id, x.Code, x.Name, x.Address, x.Phone, x.Mobile, x.BirthDate, x.IdentityNumber, x.VehiclePlate,
                x.DepartmentId,
                x.Department == null ? null : new CatalogOption(x.Department.Id, x.Department.Code, x.Department.Name),
                x.JobTitleId,
                x.JobTitle == null ? null : new CatalogOption(x.JobTitle.Id, x.JobTitle.Code, x.JobTitle.Name)))
            .ToListAsync(ct);

    public async Task SaveAsync(SaveEmployeeCommand command, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Employees, command.Id == 0);
        var item = command.Id == 0
            ? new Employee()
            : await db.FindAsync<Employee>(command.Id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        item.Code = command.Code;
        item.Name = command.Name;
        item.Address = command.Address;
        item.Phone = command.Phone;
        item.Mobile = command.Mobile;
        item.BirthDate = command.BirthDate;
        item.IdentityNumber = command.IdentityNumber;
        item.VehiclePlate = command.VehiclePlate;
        item.DepartmentId = command.DepartmentId;
        item.JobTitleId = command.JobTitleId;
        EmployeeRules.EnsureCanSave(item);
        if (await db.Employees.AnyAsync(x => x.Code == item.Code && x.Id != item.Id, ct))
            throw new InvalidOperationException("Mã nhân viên đã tồn tại.");
        if (item.DepartmentId is int departmentId && !await db.Departments.AnyAsync(x => x.Id == departmentId, ct))
            throw new InvalidOperationException("Phòng ban không tồn tại.");
        if (item.JobTitleId is int jobTitleId && !await db.JobTitles.AnyAsync(x => x.Id == jobTitleId, ct))
            throw new InvalidOperationException("Chức vụ không tồn tại.");

        item.UpdatedAt = timeProvider.GetLocalNow().DateTime;
        if (item.Id == 0) db.Add(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Employees, PermissionAction.Delete);
        var entity = await db.FindAsync<Employee>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, entity, ct);
    }
}
