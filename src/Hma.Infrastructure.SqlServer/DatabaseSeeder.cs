using Hma.Application.Common.Authorization;
using Hma.Application.Common.Security;
using Hma.Domain.Entities;
using Hma.Domain.Normalization;
using Hma.Domain.Rules;
using Microsoft.EntityFrameworkCore;

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
                new VehicleType { Code = "1.5T", Name = "Xe 1.5 tấn", Tonnage = 1.5m },
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
            ("cities", "Thành phố"),
            ("locations", "Điểm"),
            ("routes", "Tuyến"),
            ("departments", "Phòng ban"),
            ("job-titles", "Chức vụ"),
            ("price-lists", "Bảng giá"),
            ("partner-rates", "Giá mua đối tác"),
            ("partner-settlements", "Quyết toán đối tác"),
            ("transport-exceptions", "Sự cố vận tải"),
            ("dispatch-orders", "Lệnh điều xe"),
            ("dispatch-grid-edit", "Sửa lệnh theo khách"),
            ("reconcile", "Đối soát"),
            ("statements", "Bảng kê tháng"),
            ("lookup", "Tra cứu chuyến"),
            ("dashboard", "Dashboard"),
            ("reports", "Báo cáo"),
            ("users", "Người dùng"),
            ("settings", "Cấu hình")
        };
        var existingKeys = db.Screens.Select(s => s.Key).ToList();
        foreach (var (key, name) in screens)
        {
            if (!existingKeys.Contains(key))
                db.Screens.Add(new AppScreen { Key = key, Name = name });
        }
        await db.SaveChangesAsync(ct);

        if (!await db.TransportExceptionCodes.AnyAsync(ct))
        {
            db.TransportExceptionCodes.AddRange(
                new TransportExceptionCode { Code = "WAIT", Name = "Chờ bốc/dỡ hàng" },
                new TransportExceptionCode { Code = "OVERNIGHT", Name = "Lưu xe qua đêm" },
                new TransportExceptionCode { Code = "TOLL", Name = "Cầu đường phát sinh" },
                new TransportExceptionCode { Code = "RETURN", Name = "Quay đầu / đổi hành trình" },
                new TransportExceptionCode { Code = "FAILED_DELIVERY", Name = "Giao hàng thất bại" },
                new TransportExceptionCode { Code = "CLAIM", Name = "Tổn thất / khiếu nại" },
                new TransportExceptionCode { Code = "OTHER", Name = "Sự cố khác" });
            await db.SaveChangesAsync(ct);
        }

        var settingsScreen = db.Screens.FirstOrDefault(s => s.Key == ScreenKeys.Settings);
        if (settingsScreen is not null && settingsScreen.Name != "Cấu hình")
            settingsScreen.Name = "Cấu hình";
        var cityScreen = db.Screens.FirstOrDefault(s => s.Key == ScreenKeys.Cities);
        if (cityScreen is not null && cityScreen.Name != "Thành phố")
            cityScreen.Name = "Thành phố";

        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Sequences, ct))
        {
            db.Sequences.AddRange(
                new DocumentSequence { Key = "dispatch-order", LastValue = 0 },
                new DocumentSequence { Key = "freight-statement", LastValue = 0 },
                new DocumentSequence { Key = "partner-settlement", LastValue = 0 },
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
                Name = "Công ty TNHH dịch vụ vận tải và thương mại Hà Minh Anh",
                Address = "SỐ 121 - KHU 3 - QL2 - PHÚ MINH - SÓC SƠN – TP HÀ NỘI",
                Phone = "046 2544 966 8 – 0987 060 666",
                TaxCode = "010013455645",
                Email = "truckinghaco@gmail.com"
            });
        }

        await EnsurePaymentMethodAsync(db, PaymentMethodCodes.Credit, "Trả sau", ct);
        await EnsurePaymentMethodAsync(db, PaymentMethodCodes.DriverCollect, "Lái xe thu", ct);
        await EnsurePaymentMethodAsync(db, PaymentMethodCodes.DispatcherCollect, "Điều hành thu", ct);

        await db.SaveChangesAsync(ct);

        var hasher = new Pbkdf2PasswordHasher();
        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Users, ct))
        {
            var bootstrapPassword = Environment.GetEnvironmentVariable("HMA_BOOTSTRAP_ADMIN_PASSWORD");
            if (string.IsNullOrWhiteSpace(bootstrapPassword))
            {
                throw new InvalidOperationException(
                    "Cơ sở dữ liệu chưa có người dùng. Đặt biến HMA_BOOTSTRAP_ADMIN_PASSWORD bằng mật khẩu mạnh cho lần khởi tạo đầu tiên.");
            }
            AppUserRules.EnsurePasswordIsStrong(bootstrapPassword);
            db.Users.Add(new AppUser
            {
                UserName = "admin",
                PasswordHash = hasher.Hash(bootstrapPassword),
                DisplayName = "Quản trị",
                IsManager = true,
                CreatedAt = DateTime.Now
            });
            await db.SaveChangesAsync(ct);
        }

        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                db.Users.Where(u => u.UserName == "ketoan"), ct))
        {
            var accountantPassword = Environment.GetEnvironmentVariable("HMA_BOOTSTRAP_ACCOUNTANT_PASSWORD");
            if (!string.IsNullOrWhiteSpace(accountantPassword))
            {
                AppUserRules.EnsurePasswordIsStrong(accountantPassword);
                var accountant = new AppUser
                {
                    UserName = "ketoan",
                    PasswordHash = hasher.Hash(accountantPassword),
                    DisplayName = "Kế toán",
                    IsManager = false,
                    CreatedAt = DateTime.Now
                };
                db.Users.Add(accountant);
                await db.SaveChangesAsync(ct);

                var keys = new[] { "customers", "partners", "drivers", "vehicles", "employees", "cities", "locations", "routes", "departments", "job-titles",
                "price-lists", "partner-rates", "partner-settlements", "transport-exceptions", "dispatch-orders", "dispatch-grid-edit", "reconcile", "statements", "lookup", "dashboard", "reports" };
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
        }

        var accountantUser = db.Users.FirstOrDefault(u => u.UserName == "ketoan");
        await GrantAccountantScreenAsync(db, accountantUser, ScreenKeys.DispatchGridEdit, ct);
        await GrantAccountantScreenAsync(db, accountantUser, ScreenKeys.Locations, ct);
        await GrantAccountantScreenAsync(db, accountantUser, ScreenKeys.Routes, ct);
        await GrantAccountantScreenAsync(db, accountantUser, ScreenKeys.PartnerRates, ct);
        await GrantAccountantScreenAsync(db, accountantUser, ScreenKeys.PartnerSettlements, ct);
        await GrantAccountantScreenAsync(db, accountantUser, ScreenKeys.TransportExceptions, ct);
        await EnsureSequenceAsync(db, "partner-settlement", ct);
        await EnsureVehicleTypeAsync(db, "1.5T", "Xe 1.5 tấn", 1.5m, ct);
        await DemoDataSeeder.SeedIfEmptyAsync(db, ct);
        await EnsureVehicleAliasesAsync(db, ct);
    }

    private static async Task GrantAccountantScreenAsync(
        HmaDbContext db, AppUser? accountant, string screenKey, CancellationToken ct)
    {
        if (accountant is null)
            return;
        var screen = db.Screens.FirstOrDefault(s => s.Key == screenKey);
        if (screen is null || db.UserPermissions.Any(p => p.AppUserId == accountant.Id && p.AppScreenId == screen.Id))
            return;
        db.UserPermissions.Add(new UserPermission
        {
            AppUserId = accountant.Id,
            AppScreenId = screen.Id,
            CanView = true,
            CanCreate = true,
            CanUpdate = true,
            CanDelete = false,
            CanPrint = true
        });
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureVehicleTypeAsync(
        HmaDbContext db, string code, string name, decimal tonnage, CancellationToken ct)
    {
        if (await db.VehicleTypes.AnyAsync(t => t.Code == code, ct))
            return;
        db.VehicleTypes.Add(new VehicleType { Code = code, Name = name, Tonnage = tonnage });
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureSequenceAsync(HmaDbContext db, string key, CancellationToken ct)
    {
        if (await db.Sequences.AnyAsync(x => x.Key == key, ct))
            return;
        db.Sequences.Add(new DocumentSequence { Key = key, LastValue = 0 });
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsurePaymentMethodAsync(
        HmaDbContext db, string code, string name, CancellationToken ct)
    {
        var existing = await db.PaymentMethods.SingleOrDefaultAsync(x => x.Code == code, ct);
        if (existing is null)
            db.PaymentMethods.Add(new PaymentMethod { Code = code, Name = name });
        else if (existing.Name != name)
            existing.Name = name;
        else
            return;

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureVehicleAliasesAsync(HmaDbContext db, CancellationToken ct)
    {
        var vehicles = await db.Vehicles.AsNoTracking().ToListAsync(ct);
        if (vehicles.Count == 0)
            return;
        var existing = await db.VehicleAliases.AsNoTracking().Select(a => a.Alias).ToListAsync(ct);
        var added = false;
        foreach (var vehicle in vehicles)
        {
            if (!VehiclePlateRules.TryCanonicalize(vehicle.PlateNumber, out var canonical))
                continue;
            foreach (var form in VehiclePlateRules.DictionaryForms(canonical)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (existing.Any(a => AliasText.EqualsNormalized(a, form)))
                    continue;
                db.VehicleAliases.Add(new VehicleAlias { Alias = form, VehicleId = vehicle.Id });
                existing.Add(form);
                added = true;
            }
        }

        if (added)
            await db.SaveChangesAsync(ct);
    }
}
