using Hma.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Catalogs;

public sealed class CatalogOptionQueryService(IHmaDbContext db)
{
    private const int MaxLookupResults = 50;

    public Task<List<CatalogOption>> CitiesAsync(CancellationToken ct = default) =>
        db.Cities.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new CatalogOption(x.Id, x.Code, x.Name)).ToListAsync(ct);

    public Task<List<LocationOption>> LocationsAsync(CancellationToken ct = default) =>
        db.Locations.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new LocationOption(x.Id, x.Code, x.Name, x.CityId,
                x.City == null ? null : new CatalogOption(x.City.Id, x.City.Code, x.City.Name)))
            .ToListAsync(ct);

    public Task<List<RouteOption>> RoutesAsync(CancellationToken ct = default) =>
        db.Routes.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new RouteOption(x.Id, x.Code, x.Name,
                x.Stops.OrderBy(stop => stop.Sequence)
                    .Select(stop => new RouteStopOption(stop.Id, stop.Sequence, stop.LocationId,
                        stop.Location == null ? null : new LocationOption(
                            stop.Location.Id, stop.Location.Code, stop.Location.Name, stop.Location.CityId,
                            stop.Location.City == null ? null : new CatalogOption(
                                stop.Location.City.Id, stop.Location.City.Code, stop.Location.City.Name))))
                    .ToList()))
            .ToListAsync(ct);

    public Task<List<CatalogOption>> DepartmentsAsync(CancellationToken ct = default) =>
        db.Departments.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new CatalogOption(x.Id, x.Code, x.Name)).ToListAsync(ct);

    public Task<List<CatalogOption>> JobTitlesAsync(CancellationToken ct = default) =>
        db.JobTitles.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new CatalogOption(x.Id, x.Code, x.Name)).ToListAsync(ct);

    public Task<List<VehicleTypeOption>> VehicleTypesAsync(CancellationToken ct = default) =>
        db.VehicleTypes.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new VehicleTypeOption(x.Id, x.Code, x.Name, x.Tonnage)).ToListAsync(ct);

    public Task<List<PaymentMethodOption>> PaymentMethodsAsync(CancellationToken ct = default) =>
        db.PaymentMethods.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new PaymentMethodOption(x.Id, x.Code, x.Name)).ToListAsync(ct);

    public Task<List<EmployeeOption>> EmployeesAsync(CancellationToken ct = default) =>
        db.Employees.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new EmployeeOption(x.Id, x.Code, x.Name, x.DepartmentId,
                x.Department == null ? null : new CatalogOption(x.Department.Id, x.Department.Code, x.Department.Name),
                x.JobTitleId,
                x.JobTitle == null ? null : new CatalogOption(x.JobTitle.Id, x.JobTitle.Code, x.JobTitle.Name)))
            .ToListAsync(ct);

    public Task<List<PartnerOption>> PartnersAsync(CancellationToken ct = default) =>
        db.Partners.AsNoTracking().OrderBy(x => x.Code).Take(500)
            .Select(x => new PartnerOption(x.Id, x.Code, x.Name, x.OperatingFeePercent)).ToListAsync(ct);

    public Task<List<DriverOption>> DriverOptionsAsync(string? text, CancellationToken ct = default)
    {
        var query = db.Drivers.AsNoTracking().Include(x => x.Partner).AsQueryable();
        if (!string.IsNullOrWhiteSpace(text))
        {
            var term = text.Trim();
            query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term));
        }

        return query.OrderBy(x => x.Name).ThenBy(x => x.Code).Take(MaxLookupResults)
            .Select(x => new DriverOption(x.Id, x.Code, x.Name, x.Phone, x.PartnerId,
                x.Partner == null ? null : new PartnerOption(
                    x.Partner.Id, x.Partner.Code, x.Partner.Name, x.Partner.OperatingFeePercent)))
            .ToListAsync(ct);
    }

    public Task<List<VehicleOption>> VehiclesAsync(CancellationToken ct = default) =>
        db.Vehicles.AsNoTracking().Include(x => x.Partner).Include(x => x.VehicleType)
            .OrderBy(x => x.PlateNumber).Take(500)
            .Select(x => new VehicleOption(x.Id, x.PlateNumber, x.PartnerId,
                x.Partner == null ? null : new PartnerOption(
                    x.Partner.Id, x.Partner.Code, x.Partner.Name, x.Partner.OperatingFeePercent),
                x.VehicleTypeId,
                x.VehicleType == null ? null : new VehicleTypeOption(
                    x.VehicleType.Id, x.VehicleType.Code, x.VehicleType.Name, x.VehicleType.Tonnage),
                x.Tonnage))
            .ToListAsync(ct);

    public Task<List<VehicleOption>> VehicleOptionsAsync(string? text, CancellationToken ct = default)
    {
        var query = db.Vehicles.AsNoTracking().Include(x => x.Partner).Include(x => x.VehicleType).AsQueryable();
        if (!string.IsNullOrWhiteSpace(text))
        {
            var term = text.Trim();
            query = query.Where(x => x.PlateNumber.Contains(term));
        }

        return query.OrderBy(x => x.PlateNumber).Take(MaxLookupResults)
            .Select(x => new VehicleOption(x.Id, x.PlateNumber, x.PartnerId,
                x.Partner == null ? null : new PartnerOption(
                    x.Partner.Id, x.Partner.Code, x.Partner.Name, x.Partner.OperatingFeePercent),
                x.VehicleTypeId,
                x.VehicleType == null ? null : new VehicleTypeOption(
                    x.VehicleType.Id, x.VehicleType.Code, x.VehicleType.Name, x.VehicleType.Tonnage),
                x.Tonnage))
            .ToListAsync(ct);
    }
}
