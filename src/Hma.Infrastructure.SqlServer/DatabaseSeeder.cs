using Hma.Application.Services;
using Hma.Domain.Entities;

namespace Hma.Infrastructure.SqlServer;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(HmaDbContext db, CancellationToken ct = default)
    {
        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.VehicleTypes, ct))
        {
            db.VehicleTypes.AddRange(
                new VehicleType { Code = "1.25T", Name = "Xe 1.25 tấn", Tonnage = 1.25m },
                new VehicleType { Code = "3.5T", Name = "Xe 3.5 tấn", Tonnage = 3.5m },
                new VehicleType { Code = "5T", Name = "Xe 5 tấn", Tonnage = 5m },
                new VehicleType { Code = "1.45T", Name = "Xe 1.45 tấn", Tonnage = 1.45m },
                new VehicleType { Code = "8T", Name = "Xe 8 tấn", Tonnage = 8m },
                new VehicleType { Code = "2.5T", Name = "Xe 2.5 tấn", Tonnage = 2.5m },
                new VehicleType { Code = "10T", Name = "Xe 10 tấn", Tonnage = 10m },
                new VehicleType { Code = "15T", Name = "Xe 15 tấn", Tonnage = 15m });
        }

        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Partners, ct))
            db.Partners.Add(new Partner { Code = "UNASSIGNED", Name = "Chưa gán đối tác" });

        var screens = new (string Key, string Name)[]
        {
            ("customers", "Khách hàng"),
            ("partners", "Đối tác"),
            ("drivers", "Tài xế"),
            ("vehicles", "Xe"),
            ("employees", "Nhân viên"),
            ("cities", "Thành phố / hành trình"),
            ("departments", "Phòng ban"),
            ("job-titles", "Chức vụ"),
            ("price-lists", "Bảng giá"),
            ("dispatch-orders", "Lệnh điều xe"),
            ("reconcile", "Đối soát"),
            ("statements", "Bảng kê tháng"),
            ("lookup", "Tra cứu chuyến"),
            ("dashboard", "Dashboard"),
            ("reports", "Báo cáo"),
            ("users", "Người dùng"),
            ("settings", "Tham số hệ thống")
        };
        var existingKeys = db.Screens.Select(s => s.Key).ToList();
        foreach (var (key, name) in screens)
        {
            if (!existingKeys.Contains(key))
                db.Screens.Add(new AppScreen { Key = key, Name = name });
        }
        await db.SaveChangesAsync(ct);

        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Sequences, ct))
        {
            db.Sequences.AddRange(
                new DocumentSequence { Key = "dispatch-order", LastValue = 0 },
                new DocumentSequence { Key = "freight-statement", LastValue = 0 },
                new DocumentSequence { Key = "walk-in-customer", LastValue = 0 },
                new DocumentSequence { Key = "cash-receipt", LastValue = 0 },
                new DocumentSequence { Key = "cash-payment", LastValue = 0 },
                new DocumentSequence { Key = "vat-invoice", LastValue = 0 });
        }

        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Parameters, ct))
        {
            db.Parameters.AddRange(
                new SystemParameter { Key = "VatRate", Value = "10" },
                new SystemParameter { Key = "DocumentStorePath", Value = "" },
                new SystemParameter { Key = "SchemaVersion", Value = "legacy-ux-1" });
        }

        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Companies, ct))
        {
            db.Companies.Add(new Company
            {
                Name = "Công ty TNHH dịch vụ vận tải và thương mại Hà Minh Anh"
            });
        }

        await db.SaveChangesAsync(ct);

        var hasher = new Pbkdf2PasswordHasher();
        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Users, ct))
        {
            db.Users.Add(new AppUser
            {
                UserName = "admin",
                PasswordHash = hasher.Hash("admin123"),
                DisplayName = "Quản trị",
                IsManager = true,
                CreatedAt = DateTime.Now
            });
            await db.SaveChangesAsync(ct);
        }

        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                db.Users.Where(u => u.UserName == "ketoan"), ct))
        {
            var accountant = new AppUser
            {
                UserName = "ketoan",
                PasswordHash = hasher.Hash("ketoan123"),
                DisplayName = "Kế toán",
                IsManager = false,
                CreatedAt = DateTime.Now
            };
            db.Users.Add(accountant);
            await db.SaveChangesAsync(ct);

            var keys = new[] { "customers", "partners", "drivers", "vehicles", "employees", "cities", "departments", "job-titles",
                "price-lists", "dispatch-orders", "reconcile", "statements", "lookup", "dashboard", "reports" };
            var granted = db.Screens.Where(s => keys.Contains(s.Key)).ToList();
            foreach (var screen in granted)
            {
                db.UserPermissions.Add(new UserPermission
                {
                    AppUserId = accountant.Id,
                    AppScreenId = screen.Id,
                    CanView = true,
                    CanCreate = screen.Key is not "dashboard" and not "reports",
                    CanUpdate = screen.Key is not "dashboard" and not "reports",
                    CanDelete = false,
                    CanPrint = true
                });
            }
            await db.SaveChangesAsync(ct);
        }

        await DemoDataSeeder.SeedIfEmptyAsync(db, ct);
    }
}
