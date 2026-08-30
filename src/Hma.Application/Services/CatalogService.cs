using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class CatalogService(IHmaDbContext db, ICurrentUser current)
{
    public Task<List<City>> CitiesAsync(CancellationToken ct = default) =>
        db.Cities.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);

    public Task<List<Department>> DepartmentsAsync(CancellationToken ct = default) =>
        db.Departments.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);

    public Task<List<JobTitle>> JobTitlesAsync(CancellationToken ct = default) =>
        db.JobTitles.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);

    public Task<List<VehicleType>> VehicleTypesAsync(CancellationToken ct = default) =>
        db.VehicleTypes.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);

    public Task<List<PaymentMethod>> PaymentMethodsAsync(CancellationToken ct = default) =>
        db.PaymentMethods.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);

    public Task<List<Employee>> EmployeesAsync(CancellationToken ct = default) =>
        db.Employees.AsNoTracking().Include(e => e.Department).Include(e => e.JobTitle).OrderBy(e => e.Code).ToListAsync(ct);

    public Task<List<Partner>> PartnersAsync(string? code = null, string? name = null, CancellationToken ct = default)
    {
        var q = db.Partners.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) q = q.Where(p => p.Code.Contains(code));
        if (!string.IsNullOrWhiteSpace(name)) q = q.Where(p => p.Name.Contains(name));
        return q.OrderBy(p => p.Code).Take(500).ToListAsync(ct);
    }

    public Task<List<Driver>> DriversAsync(string? code = null, string? name = null, CancellationToken ct = default)
    {
        var q = db.Drivers.AsNoTracking().Include(d => d.Partner).AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) q = q.Where(d => d.Code.Contains(code));
        if (!string.IsNullOrWhiteSpace(name)) q = q.Where(d => d.Name.Contains(name));
        return q.OrderBy(d => d.Code).Take(500).ToListAsync(ct);
    }

    public Task<List<Vehicle>> VehiclesAsync(string? plate = null, CancellationToken ct = default)
    {
        var q = db.Vehicles.AsNoTracking().Include(v => v.Partner).Include(v => v.VehicleType).AsQueryable();
        if (!string.IsNullOrWhiteSpace(plate)) q = q.Where(v => v.PlateNumber.Contains(plate));
        return q.OrderBy(v => v.PlateNumber).Take(500).ToListAsync(ct);
    }

    public async Task SaveCityAsync(City city, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Cities, city.Id == 0);
        if (city.Id == 0) db.Add(city);
        else db.Update(city);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveDepartmentAsync(Department item, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Departments, item.Id == 0);
        if (item.Id == 0) db.Add(item);
        else db.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveJobTitleAsync(JobTitle item, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.JobTitles, item.Id == 0);
        if (item.Id == 0) db.Add(item);
        else db.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveEmployeeAsync(Employee item, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Employees, item.Id == 0);
        EmployeeRules.EnsureCanSave(item);
        item.UpdatedAt = DateTime.Now;
        if (item.Id == 0) db.Add(item);
        else db.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task SavePartnerAsync(Partner item, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Partners, item.Id == 0);
        PartnerRules.EnsureCanSave(item);
        if (await db.Partners.AnyAsync(p => p.Code == item.Code && p.Id != item.Id, ct))
            throw new InvalidOperationException("Mã đối tác đã tồn tại.");
        if (item.Id == 0) db.Add(item);
        else db.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveDriverAsync(Driver item, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Drivers, item.Id == 0);
        DriverRules.EnsureCanSave(item);
        if (await db.Drivers.AnyAsync(d => d.Code == item.Code && d.Id != item.Id, ct))
            throw new InvalidOperationException("Mã tài xế đã tồn tại.");
        if (item.Id == 0) db.Add(item);
        else db.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveVehicleAsync(Vehicle item, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Vehicles, item.Id == 0);
        if (string.IsNullOrWhiteSpace(item.PlateNumber))
            throw new InvalidOperationException("Biển số xe là bắt buộc.");
        if (item.PartnerId == 0)
            throw new InvalidOperationException("Xe phải thuộc một đối tác.");
        if (await db.Vehicles.AnyAsync(v => v.PlateNumber == item.PlateNumber && v.Id != item.Id, ct))
            throw new InvalidOperationException("Biển số đã tồn tại.");
        if (item.Id == 0) db.Add(item);
        else db.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync<T>(int id, CancellationToken ct = default) where T : class
    {
        PermissionGuard.Require(current, ScreenKeyFor<T>(), PermissionAction.Delete);
        var entity = await db.FindAsync<T>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, ct);
    }

    public Task<List<DispatchOrder>> TripsByVehicleAsync(int vehicleId, CancellationToken ct = default) =>
        db.DispatchOrders.AsNoTracking().Include(d => d.Customer).Include(d => d.DeliveryCity).Include(d => d.Driver)
            .Where(d => d.VehicleId == vehicleId)
            .OrderByDescending(d => d.PickupAt).Take(200).ToListAsync(ct);

    public Task<List<DispatchOrder>> TripsByDriverAsync(int driverId, CancellationToken ct = default) =>
        db.DispatchOrders.AsNoTracking().Include(d => d.Customer).Include(d => d.DeliveryCity).Include(d => d.Vehicle)
            .Where(d => d.DriverId == driverId)
            .OrderByDescending(d => d.PickupAt).Take(200).ToListAsync(ct);

    private static string ScreenKeyFor<T>() => typeof(T).Name switch
    {
        nameof(City) => ScreenKeys.Cities,
        nameof(Department) => ScreenKeys.Departments,
        nameof(JobTitle) => ScreenKeys.JobTitles,
        nameof(Employee) => ScreenKeys.Employees,
        nameof(Partner) => ScreenKeys.Partners,
        nameof(Driver) => ScreenKeys.Drivers,
        nameof(Vehicle) => ScreenKeys.Vehicles,
        _ => throw new InvalidOperationException("Không xác định được quyền xóa.")
    };
}
