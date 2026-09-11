using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Infrastructure.SqlServer;

/// <summary>
/// Danh mục diễn tập từ bảng điều xe 11/08/2026. Chỉ chạy khi chưa có khách — không đè ETL/production.
/// Không seed lệnh, bảng giá, hay bảng kê.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedIfEmptyAsync(HmaDbContext db, CancellationToken ct = default)
    {
        if (await db.Customers.AnyAsync(ct))
            return;

        var dieuPhoi = Dept(db, "DP", "Điều phối");
        var keToan = Dept(db, "KT", "Kế toán");
        var banGd = Dept(db, "BGĐ", "Ban giám đốc");
        var nv = Title(db, "NV", "Nhân viên");
        var ktv = Title(db, "KTV", "Kế toán viên");
        var tp = Title(db, "TP", "Trưởng phòng");
        var gd = Title(db, "GD", "Giám đốc");
        await db.SaveChangesAsync(ct);

        Emp(db, "NV001", "Trần Văn Minh", "0904 512 378", dieuPhoi, tp,
            "Tổ 4, phường Bồ Đề, Long Biên, Hà Nội", new DateTime(1988, 3, 12), "001088003412");
        var hoa = Emp(db, "NV002", "Nguyễn Thị Hoa", "0912 886 441", keToan, ktv,
            "Ngõ 128, đường Giải Phóng, Hoàng Mai, Hà Nội", new DateTime(1992, 7, 21), "001192007821");
        Emp(db, "NV003", "Lê Thị Lan", "0983 227 190", keToan, nv,
            "KĐT Việt Hưng, Long Biên, Hà Nội", new DateTime(1995, 11, 5), "001195011105");
        var huy = Emp(db, "NV004", "Phạm Quốc Huy", "0903 218 009", banGd, gd,
            "Phố Nguyễn Khoái, Hai Bà Trưng, Hà Nội", new DateTime(1979, 1, 18), "001079001118");
        await db.SaveChangesAsync(ct);

        var admin = await db.Users.FirstOrDefaultAsync(u => u.UserName == "admin", ct);
        var ketoan = await db.Users.FirstOrDefaultAsync(u => u.UserName == "ketoan", ct);
        if (admin is not null)
        {
            admin.DisplayName = "Phạm Quốc Huy";
            admin.EmployeeId = huy.Id;
        }
        if (ketoan is not null)
        {
            ketoan.DisplayName = "Nguyễn Thị Hoa";
            ketoan.EmployeeId = hoa.Id;
        }
        await db.SaveChangesAsync(ct);

        var unassigned = await db.Partners.FirstAsync(p => p.Code == "UNASSIGNED", ct);
        var types = await db.VehicleTypes.ToListAsync(ct);

        var customers = new List<Customer>();
        foreach (var (code, _) in LegacyData.Customers)
        {
            var customer = new Customer { Code = code, Name = code, UpdatedAt = DateTime.Today };
            db.Customers.Add(customer);
            customers.Add(customer);
        }

        await db.SaveChangesAsync(ct);
        var customerAliasSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < LegacyData.Customers.Length; i++)
        {
            var customer = customers[i];
            foreach (var alias in LegacyData.Customers[i].Aliases)
            {
                if (AliasText.EqualsNormalized(alias, customer.Code) || AliasText.EqualsNormalized(alias, customer.Name))
                    continue;
                var text = AliasText.Normalize(alias);
                if (text.Length == 0 || text.Length > AliasText.MaxLength || !customerAliasSeen.Add(text))
                    continue;
                db.CustomerAliases.Add(new CustomerAlias { Alias = text, CustomerId = customer.Id });
            }
        }

        var locByCode = new Dictionary<string, Location>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in LegacyData.Locations.GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
        {
            var (code, name, _) = group.First();
            var location = new Location { Code = code, Name = name };
            db.Locations.Add(location);
            locByCode[code] = location;
        }

        await db.SaveChangesAsync(ct);
        var locationAliasSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in LegacyData.Locations.GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
        {
            var code = group.Key;
            var location = locByCode[code];
            foreach (var alias in group.SelectMany(x => x.Aliases))
            {
                if (AliasText.EqualsNormalized(alias, location.Code) || AliasText.EqualsNormalized(alias, location.Name))
                    continue;
                var text = AliasText.Normalize(alias);
                if (text.Length == 0 || text.Length > AliasText.MaxLength || !locationAliasSeen.Add(text))
                    continue;
                db.LocationAliases.Add(new LocationAlias
                {
                    Alias = text,
                    LocationId = location.Id,
                    Kind = LocationAliasKind.Both
                });
            }
        }

        var routeByFingerprint = new Dictionary<string, Route>(StringComparer.Ordinal);
        var routeN = 1;
        foreach (var (stopCodes, _) in LegacyData.Routes)
        {
            var stops = new List<Location>();
            var missing = false;
            foreach (var code in stopCodes)
            {
                if (!locByCode.TryGetValue(code, out var loc))
                {
                    missing = true;
                    break;
                }

                stops.Add(loc);
            }

            if (missing || stops.Count < 2)
                continue;

            var fingerprint = RouteFingerprint.From(stops.Select(s => s.Id));
            if (routeByFingerprint.ContainsKey(fingerprint))
                continue;

            var route = new Route
            {
                Code = $"RT{routeN:000}",
                Name = RouteFingerprint.FormatLabel(stops.Select(s => s.Name)),
                Fingerprint = fingerprint
            };
            routeN++;
            for (var i = 0; i < stops.Count; i++)
                route.Stops.Add(new RouteStop { Sequence = i, LocationId = stops[i].Id });
            db.Routes.Add(route);
            routeByFingerprint[fingerprint] = route;
        }

        await db.SaveChangesAsync(ct);

        var routeAliasSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (stopCodes, aliases) in LegacyData.Routes)
        {
            var ids = new List<int>();
            var missing = false;
            foreach (var code in stopCodes)
            {
                if (!locByCode.TryGetValue(code, out var loc))
                {
                    missing = true;
                    break;
                }

                ids.Add(loc.Id);
            }

            if (missing || ids.Count < 2)
                continue;
            var fingerprint = RouteFingerprint.From(ids);
            if (!routeByFingerprint.TryGetValue(fingerprint, out var route))
                continue;

            foreach (var alias in aliases)
            {
                var text = AliasText.Normalize(alias);
                if (text.Length == 0 || text.Length > AliasText.MaxLength)
                    continue;
                if (AliasText.EqualsNormalized(text, route.Name) || AliasText.EqualsNormalized(text, route.Code))
                    continue;
                if (!routeAliasSeen.Add(text))
                    continue;
                db.RouteAliases.Add(new RouteAlias { Alias = text, RouteId = route.Id });
            }
        }

        var driverN = 1;
        foreach (var name in LegacyData.Drivers)
        {
            db.Drivers.Add(new Driver
            {
                Code = $"TX{driverN:000}",
                Name = name,
                PartnerId = unassigned.Id
            });
            driverN++;
        }

        foreach (var (plate, typeCode) in LegacyData.Vehicles)
        {
            var type = typeCode is null
                ? null
                : types.FirstOrDefault(t => t.Code.Equals(typeCode, StringComparison.OrdinalIgnoreCase));
            db.Vehicles.Add(new Vehicle
            {
                PlateNumber = plate,
                PartnerId = unassigned.Id,
                VehicleTypeId = type?.Id,
                Tonnage = type?.Tonnage
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static Department Dept(HmaDbContext db, string code, string name)
    {
        var row = new Department { Code = code, Name = name };
        db.Departments.Add(row);
        return row;
    }

    private static JobTitle Title(HmaDbContext db, string code, string name)
    {
        var row = new JobTitle { Code = code, Name = name };
        db.JobTitles.Add(row);
        return row;
    }

    private static Employee Emp(HmaDbContext db, string code, string name, string mobile,
        Department dept, JobTitle title, string address, DateTime birth, string cccd)
    {
        var row = new Employee
        {
            Code = code,
            Name = name,
            Mobile = mobile,
            Phone = mobile,
            DepartmentId = dept.Id,
            JobTitleId = title.Id,
            Address = address,
            BirthDate = birth,
            IdentityNumber = cccd,
            UpdatedAt = DateTime.Today
        };
        db.Employees.Add(row);
        return row;
    }
}
