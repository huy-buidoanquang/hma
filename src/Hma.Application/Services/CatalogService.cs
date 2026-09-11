using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class CatalogService(IHmaDbContext db, ICurrentUser current)
{
    private const int MaxListResults = 500;
    private const int MaxLookupResults = 50;

    public Task<List<City>> CitiesAsync(CancellationToken ct = default) =>
        db.Cities.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);

    public Task<List<Location>> LocationsAsync(CancellationToken ct = default) =>
        db.Locations.AsNoTracking().Include(l => l.City).OrderBy(x => x.Code).ToListAsync(ct);

    public Task<List<Route>> RoutesAsync(CancellationToken ct = default) =>
        db.Routes.AsNoTracking().Include(r => r.Stops).ThenInclude(s => s.Location).OrderBy(x => x.Code).ToListAsync(ct);

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
        return q.OrderBy(p => p.Code).Take(MaxListResults).ToListAsync(ct);
    }

    public Task<List<Driver>> DriversAsync(string? code = null, string? name = null, CancellationToken ct = default)
    {
        var q = db.Drivers.AsNoTracking().Include(d => d.Partner).AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) q = q.Where(d => d.Code.Contains(code));
        if (!string.IsNullOrWhiteSpace(name)) q = q.Where(d => d.Name.Contains(name));
        return q.OrderBy(d => d.Code).Take(MaxListResults).ToListAsync(ct);
    }

    public Task<List<Driver>> DriverOptionsAsync(string? text, CancellationToken ct = default)
    {
        var q = db.Drivers.AsNoTracking().Include(d => d.Partner).AsQueryable();
        if (!string.IsNullOrWhiteSpace(text))
        {
            var term = text.Trim();
            q = q.Where(d => d.Code.Contains(term) || d.Name.Contains(term));
        }

        return q.OrderBy(d => d.Name).ThenBy(d => d.Code).Take(MaxLookupResults).ToListAsync(ct);
    }

    public Task<List<Vehicle>> VehiclesAsync(string? plate = null, CancellationToken ct = default)
    {
        var q = db.Vehicles.AsNoTracking().Include(v => v.Partner).Include(v => v.VehicleType).AsQueryable();
        if (!string.IsNullOrWhiteSpace(plate)) q = q.Where(v => v.PlateNumber.Contains(plate));
        return q.OrderBy(v => v.PlateNumber).Take(MaxListResults).ToListAsync(ct);
    }

    public Task<List<Vehicle>> VehicleOptionsAsync(string? text, CancellationToken ct = default)
    {
        var q = db.Vehicles.AsNoTracking().Include(v => v.Partner).Include(v => v.VehicleType).AsQueryable();
        if (!string.IsNullOrWhiteSpace(text))
        {
            var term = text.Trim();
            q = q.Where(v => v.PlateNumber.Contains(term));
        }

        return q.OrderBy(v => v.PlateNumber).Take(MaxLookupResults).ToListAsync(ct);
    }

    public async Task SaveCityAsync(City city, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Cities, city.Id == 0);
        CatalogItemDomainService.EnsureCanSave(city.Code, city.Name, "thành phố");
        if (await db.Cities.AnyAsync(c => c.Code == city.Code && c.Id != city.Id, ct))
            throw new InvalidOperationException("Mã thành phố đã tồn tại.");
        if (city.Id == 0) db.Add(city);
        else db.Update(city);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task SaveDepartmentAsync(Department item, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Departments, item.Id == 0);
        CatalogItemDomainService.EnsureCanSave(item.Code, item.Name, "phòng ban");
        if (await db.Departments.AnyAsync(d => d.Code == item.Code && d.Id != item.Id, ct))
            throw new InvalidOperationException("Mã phòng ban đã tồn tại.");
        if (item.Id == 0) db.Add(item);
        else db.Update(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task SaveJobTitleAsync(JobTitle item, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.JobTitles, item.Id == 0);
        CatalogItemDomainService.EnsureCanSave(item.Code, item.Name, "chức vụ");
        if (await db.JobTitles.AnyAsync(j => j.Code == item.Code && j.Id != item.Id, ct))
            throw new InvalidOperationException("Mã chức vụ đã tồn tại.");
        if (item.Id == 0) db.Add(item);
        else db.Update(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task SaveEmployeeAsync(Employee item, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Employees, item.Id == 0);
        EmployeeRules.EnsureCanSave(item);
        if (await db.Employees.AnyAsync(e => e.Code == item.Code && e.Id != item.Id, ct))
            throw new InvalidOperationException("Mã nhân viên đã tồn tại.");
        if (item.DepartmentId is int departmentId
            && !await db.Departments.AnyAsync(d => d.Id == departmentId, ct))
            throw new InvalidOperationException("Phòng ban không tồn tại.");
        if (item.JobTitleId is int jobTitleId
            && !await db.JobTitles.AnyAsync(j => j.Id == jobTitleId, ct))
            throw new InvalidOperationException("Chức vụ không tồn tại.");
        var keepDepartmentId = item.DepartmentId;
        var keepJobTitleId = item.JobTitleId;
        item.Department = null;
        item.JobTitle = null;
        item.DepartmentId = keepDepartmentId;
        item.JobTitleId = keepJobTitleId;
        item.UpdatedAt = DateTime.Now;
        if (item.Id == 0) db.Add(item);
        else db.Update(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task SavePartnerAsync(Partner item, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Partners, item.Id == 0);
        PartnerRules.EnsureCanSave(item);
        if (await db.Partners.AnyAsync(p => p.Code == item.Code && p.Id != item.Id, ct))
            throw new InvalidOperationException("Mã đối tác đã tồn tại.");
        if (item.Id == 0) db.Add(item);
        else db.Update(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task SaveDriverAsync(Driver item, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Drivers, item.Id == 0);
        DriverRules.EnsureCanSave(item);
        if (await db.Drivers.AnyAsync(d => d.Code == item.Code && d.Id != item.Id, ct))
            throw new InvalidOperationException("Mã tài xế đã tồn tại.");
        if (!await db.Partners.AnyAsync(p => p.Id == item.PartnerId, ct))
            throw new InvalidOperationException("Đối tác không tồn tại.");
        var partnerId = item.PartnerId;
        item.Partner = null;
        item.PartnerId = partnerId;
        if (item.Id == 0) db.Add(item);
        else db.Update(item);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task SaveVehicleAsync(Vehicle item, CancellationToken ct = default)
    {
        PermissionGuard.RequireSave(current, ScreenKeys.Vehicles, item.Id == 0);
        VehicleDomainService.EnsureCanSave(item);
        var plateKey = DispatchImportMatching.NormalizePlate(item.PlateNumber);
        var others = await db.Vehicles.AsNoTracking()
            .Where(v => v.Id != item.Id)
            .Select(v => v.PlateNumber)
            .ToListAsync(ct);
        if (others.Any(existing => DispatchImportMatching.NormalizePlate(existing) == plateKey))
            throw new InvalidOperationException("Biển số đã tồn tại.");
        if (!await db.Partners.AnyAsync(p => p.Id == item.PartnerId, ct))
            throw new InvalidOperationException("Đối tác không tồn tại.");
        if (item.VehicleTypeId is int typeId
            && !await db.VehicleTypes.AnyAsync(t => t.Id == typeId, ct))
            throw new InvalidOperationException("Loại xe không tồn tại.");
        var keepPartnerId = item.PartnerId;
        var keepTypeId = item.VehicleTypeId;
        item.Partner = null;
        item.VehicleType = null;
        item.PartnerId = keepPartnerId;
        item.VehicleTypeId = keepTypeId;
        if (item.Id == 0) db.Add(item);
        else db.Update(item);
        await PersistenceGuard.SaveAsync(db, ct);
        await SyncVehicleAliasesAsync(item, ct);
    }

    private async Task SyncVehicleAliasesAsync(Vehicle item, CancellationToken ct)
    {
        var wanted = VehiclePlateRules.DictionaryForms(item.PlateNumber)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var current = await db.VehicleAliases.Where(a => a.VehicleId == item.Id).ToListAsync(ct);
        foreach (var extra in current.Where(a => wanted.All(w => !AliasText.EqualsNormalized(w, a.Alias))))
            db.Remove(extra);
        var taken = await db.VehicleAliases.AsNoTracking()
            .Where(a => a.VehicleId != item.Id)
            .Select(a => a.Alias)
            .ToListAsync(ct);
        foreach (var form in wanted)
        {
            if (current.Any(a => AliasText.EqualsNormalized(a.Alias, form)))
                continue;
            if (taken.Any(a => AliasText.EqualsNormalized(a, form)))
                throw new InvalidOperationException($"Bí danh biển «{form}» đã gắn với xe khác.");
            db.Add(new VehicleAlias { Alias = form, VehicleId = item.Id });
        }
        await PersistenceGuard.SaveAsync(db, ct);
    }

    public async Task DeleteAsync<T>(int id, CancellationToken ct = default) where T : class
    {
        PermissionGuard.Require(current, ScreenKeyFor<T>(), PermissionAction.Delete);
        var entity = await db.FindAsync<T>(id, ct) ?? throw new InvalidOperationException("Không tìm thấy bản ghi.");
        db.Remove(entity);
        await ReferentialConflict.SaveAsync(db, ct);
    }

    public Task<List<DispatchOrder>> TripsByVehicleAsync(int vehicleId, CancellationToken ct = default) =>
        db.DispatchOrders.AsNoTracking().Include(d => d.Customer).Include(d => d.Route).Include(d => d.Stops).Include(d => d.Driver)
            .Where(d => d.VehicleId == vehicleId)
            .OrderByDescending(d => d.PickupAt).Take(200).ToListAsync(ct);

    public Task<List<DispatchOrder>> TripsByDriverAsync(int driverId, CancellationToken ct = default) =>
        db.DispatchOrders.AsNoTracking().Include(d => d.Customer).Include(d => d.Route).Include(d => d.Stops).Include(d => d.Vehicle)
            .Where(d => d.DriverId == driverId)
            .OrderByDescending(d => d.PickupAt).Take(200).ToListAsync(ct);

    private static string ScreenKeyFor<T>() => typeof(T).Name switch
    {
        nameof(City) => ScreenKeys.Cities,
        nameof(Location) => ScreenKeys.Locations,
        nameof(Route) => ScreenKeys.Routes,
        nameof(Department) => ScreenKeys.Departments,
        nameof(JobTitle) => ScreenKeys.JobTitles,
        nameof(Employee) => ScreenKeys.Employees,
        nameof(Partner) => ScreenKeys.Partners,
        nameof(Driver) => ScreenKeys.Drivers,
        nameof(Vehicle) => ScreenKeys.Vehicles,
        _ => throw new InvalidOperationException("Không xác định được quyền xóa.")
    };
}
