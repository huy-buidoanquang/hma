using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class DispatchImportService(
    IHmaDbContext db,
    DispatchOrderService orders,
    PriceListService prices,
    ICurrentUser current)
{
    public async Task<DispatchImportCheckResult> CheckAsync(
        IReadOnlyList<DispatchImportRow> rows, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Create);
        if (rows.Count == 0)
        {
            return new DispatchImportCheckResult
            {
                FileError = "File không có dòng lệnh nào để kiểm tra.",
                ValidCount = 0,
                ErrorCount = 0
            };
        }

        var checks = await CheckRowsAsync(rows, ct);
        return new DispatchImportCheckResult
        {
            Rows = checks,
            ValidCount = checks.Count(c => c.IsValid),
            ErrorCount = checks.Count(c => !c.IsValid)
        };
    }

    public async Task<DispatchImportResult> ImportAsync(
        IReadOnlyList<DispatchImportRow> rows, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.DispatchOrders, PermissionAction.Create);
        if (rows.Count == 0)
        {
            return new DispatchImportResult
            {
                Saved = 0,
                Errors = [new DispatchImportRowError { ExcelRow = 1, Message = "File không có dòng lệnh nào để nhập." }]
            };
        }

        var ctx = await LoadContextAsync(rows, ct);
        var seen = new Dictionary<TripKey, string>();
        var checks = new List<DispatchImportCheck>();
        var errors = new List<DispatchImportRowError>();
        var saved = 0;

        foreach (var row in rows)
        {
            var (issues, key, order) = await EvaluateAsync(row, ctx, seen, ct);
            checks.Add(new DispatchImportCheck { Row = row, Errors = issues });
            if (issues.Count > 0)
            {
                errors.Add(ToError(row, issues));
                continue;
            }

            if (order is null)
            {
                issues = ["Không tạo được lệnh từ dòng này."];
                checks[^1] = new DispatchImportCheck { Row = row, Errors = issues };
                errors.Add(ToError(row, issues));
                continue;
            }

            try
            {
                await orders.SaveAsync(order, ct);
                saved++;
                if (key is { } k)
                    seen[k] = Describe(row);
            }
            catch (Exception ex)
            {
                var message = PersistenceGuard.Translate(ex).Message;
                checks[^1] = new DispatchImportCheck { Row = row, Errors = [message] };
                errors.Add(ToError(row, [message]));
            }
        }

        return new DispatchImportResult { Saved = saved, Checks = checks, Errors = errors };
    }

    private async Task<List<DispatchImportCheck>> CheckRowsAsync(
        IReadOnlyList<DispatchImportRow> rows, CancellationToken ct)
    {
        var ctx = await LoadContextAsync(rows, ct);
        var seen = new Dictionary<TripKey, string>();
        var checks = new List<DispatchImportCheck>(rows.Count);
        foreach (var row in rows)
        {
            var (issues, key, _) = await EvaluateAsync(row, ctx, seen, ct);
            if (issues.Count == 0 && key is { } k)
                seen[k] = Describe(row);
            checks.Add(new DispatchImportCheck { Row = row, Errors = issues });
        }

        return checks;
    }

    private async Task<ImportContext> LoadContextAsync(IReadOnlyList<DispatchImportRow> rows, CancellationToken ct)
    {
        var customers = await db.Customers.AsNoTracking().ToListAsync(ct);
        var locations = await db.Locations.AsNoTracking().ToListAsync(ct);
        var routes = await db.Routes.AsNoTracking().Include(r => r.Stops).ToListAsync(ct);
        var customerAliases = await db.CustomerAliases.AsNoTracking().Include(a => a.Customer).ToListAsync(ct);
        var locationAliases = await db.LocationAliases.AsNoTracking().Include(a => a.Location).ToListAsync(ct);
        var routeAliases = await db.RouteAliases.AsNoTracking().Include(a => a.Route).ToListAsync(ct);
        var vehicles = await db.Vehicles.AsNoTracking().Include(v => v.VehicleType).Include(v => v.Partner).ToListAsync(ct);
        var vehicleAliases = await db.VehicleAliases.AsNoTracking().Include(a => a.Vehicle).ToListAsync(ct);
        var drivers = await db.Drivers.AsNoTracking().ToListAsync(ct);
        var payments = await db.PaymentMethods.AsNoTracking().ToListAsync(ct);
        var vehicleTypes = await db.VehicleTypes.AsNoTracking().ToListAsync(ct);

        var days = new List<DateTime>();
        foreach (var row in rows)
        {
            try { days.Add(DispatchImportMatching.ParseDate(row.PickupAt).Date); }
            catch (InvalidOperationException) { /* checked per row */ }
        }

        List<ExistingTrip> existing = [];
        if (days.Count > 0)
        {
            var from = days.Min();
            var to = days.Max().AddDays(1);
            existing = await db.DispatchOrders.AsNoTracking()
                .Where(o => o.PickupAt >= from && o.PickupAt < to
                            && o.VehicleId != null && o.RouteId != null && o.CustomerId != null)
                .Select(o => new ExistingTrip(
                    o.PickupAt,
                    o.VehicleId!.Value,
                    o.RouteId!.Value,
                    o.CustomerId!.Value,
                    o.Code))
                .ToListAsync(ct);
        }

        return new ImportContext(customers, locations, routes, customerAliases, locationAliases, routeAliases, vehicles, vehicleAliases, drivers, payments, vehicleTypes, existing);
    }

    private async Task<(List<string> Errors, TripKey? Key, DispatchOrder? Order)> EvaluateAsync(
        DispatchImportRow row,
        ImportContext ctx,
        Dictionary<TripKey, string> seen,
        CancellationToken ct)
    {
        var errors = new List<string>();
        var opsBoard = row.StartColumn > 0;
        if (opsBoard && string.IsNullOrWhiteSpace(row.Stt))
            errors.Add("Thiếu STT.");

        Route? route = null;
        if (opsBoard && string.IsNullOrWhiteSpace(row.Route))
            errors.Add("Thiếu tuyến đường.");
        else
        {
            var resolved = DispatchImportMatching.ResolveRoute(
                row.Route, row.PickupCity, row.DeliveryCity,
                ctx.Routes, ctx.RouteAliases, ctx.Locations, ctx.LocationAliases);
            route = resolved.Route;
            errors.AddRange(resolved.Errors);
        }

        DateTime pickupAt = default;
        try { pickupAt = DispatchImportMatching.ParseDate(row.PickupAt); }
        catch (InvalidOperationException ex) { errors.Add(ex.Message); }

        var customer = DispatchImportMatching.MatchCustomer(ctx.Customers, ctx.CustomerAliases, row.CustomerCode);
        if (string.IsNullOrWhiteSpace(row.CustomerCode))
            errors.Add("Thiếu khách hàng.");
        else if (customer is null)
            errors.Add($"Không tìm thấy khách hàng «{row.CustomerCode.Trim()}». Thêm bí danh tại Cấu hình → Từ điển khách.");

        var vehicle = DispatchImportMatching.MatchVehicle(ctx.Vehicles, ctx.VehicleAliases, row.Plate);
        if (string.IsNullOrWhiteSpace(row.Plate))
            errors.Add("Thiếu biển kiểm soát.");
        else if (vehicle is null)
            errors.Add($"Không tìm thấy biển kiểm soát «{row.Plate.Trim()}».");

        Driver? driver = null;
        if (string.IsNullOrWhiteSpace(row.DriverName))
            errors.Add("Thiếu tên lái xe.");
        else
        {
            driver = DispatchImportMatching.MatchDriver(ctx.Drivers, row.DriverName, vehicle?.PartnerId ?? 0);
            if (driver is null)
                errors.Add($"Không tìm thấy tài xế «{row.DriverName.Trim()}».");
        }

        var payment = DispatchImportMatching.MatchPayment(ctx.Payments, row.PaymentMethod);

        decimal extra = 0;
        try { extra = DispatchImportMatching.ParseMoney(row.ExtraCost, "Phát sinh"); }
        catch (InvalidOperationException ex) { errors.Add(ex.Message); }

        decimal? freight = null;
        if (!string.IsNullOrWhiteSpace(row.Freight))
        {
            if (!DispatchImportMatching.TryParseMoney(row.Freight, out var parsed) || parsed < 0)
                errors.Add("Cước / thành tiền không phải số.");
            else if (parsed > 0)
                freight = parsed;
        }

        int? vehicleTypeId = vehicle?.VehicleTypeId;
        if (DispatchImportTonnage.TryParse(row.Tonnage, out var tons))
        {
            var type = ctx.VehicleTypes.FirstOrDefault(t => t.Tonnage == tons);
            if (type is null)
                errors.Add($"Không tìm thấy loại xe {tons.ToString("0.##")} tấn.");
            else
                vehicleTypeId = type.Id;
        }

        if (errors.Count > 0 || customer is null || route is null || vehicle is null || driver is null
            || pickupAt == default)
            return (errors, null, null);

        var key = new TripKey(pickupAt.Date, vehicle.Id, route.Id, customer.Id);
        var existing = ctx.Existing.FirstOrDefault(e =>
            e.PickupAt.Date == key.Day && e.VehicleId == key.VehicleId && e.RouteId == key.RouteId
            && e.CustomerId == key.CustomerId);
        if (existing is not null)
            errors.Add($"Đã có lệnh {existing.Code} cùng ngày, xe, tuyến và khách.");
        else if (seen.TryGetValue(key, out var other))
            errors.Add($"Trùng chuyến trong file ({other}).");

        decimal unit = freight ?? 0;
        decimal surcharge = 0;
        FreightQuote? appliedQuote = null;
        if (freight is null or 0)
        {
            var quote = await prices.GetFreightAsync(customer.Id, route.Id, vehicleTypeId, pickupAt, ct);
            if (quote is null)
                errors.Add("Để trống cước và không khớp bảng giá (tuyến × loại xe).");
            else
            {
                appliedQuote = quote;
                unit = quote.UnitPrice;
                surcharge = quote.Surcharge;
            }
        }

        if (errors.Count > 0)
            return (errors, key, null);

        var order = new DispatchOrder
        {
            Code = row.Code?.Trim() ?? "",
            PickupAt = pickupAt,
            CustomerId = customer.Id,
            SenderCustomerId = customer.Id,
            RouteId = route.Id,
            VehicleId = vehicle.Id,
            VehicleTypeId = vehicleTypeId,
            DriverId = driver.Id,
            PaymentMethodId = payment?.Id,
            Notes = ComposeNotes(row),
            ExtraCost = extra,
            UnitPrice = unit,
            Surcharge = surcharge,
            PriceListItemId = appliedQuote?.PriceListItemId,
            PriceSourceSnapshot = appliedQuote?.SourceLabel,
            IsFreightManual = appliedQuote is null,
            FreightOverrideReason = appliedQuote is null ? "Cước nhập từ bảng điều xe Excel." : null,
            Status = DispatchStatus.Issued,
            CreatedAt = DateTime.Now,
            CreatedByUserId = current.User?.Id
        };
        BillingPeriodRules.ApplyDefault(order);
        order.RecalculateTotal();
        return (errors, key, order);
    }

    private static string? ComposeNotes(DispatchImportRow row)
    {
        var extra = row.Notes?.Trim();
        if (DispatchImportTonnage.TryParse(extra, out _))
            extra = null;
        var route = string.IsNullOrWhiteSpace(row.Route) ? null : row.Route.Trim();
        if (string.IsNullOrWhiteSpace(route))
            return extra;
        if (string.IsNullOrWhiteSpace(extra))
            return route;
        return $"{route}; {extra}";
    }

    private static DispatchImportRowError ToError(DispatchImportRow row, IReadOnlyList<string> issues) => new()
    {
        ExcelRow = row.ExcelRow,
        SheetName = row.SheetName,
        BlockLabel = row.BlockLabel,
        Plate = row.Plate,
        Message = string.Join(" ", issues)
    };

    private static string Describe(DispatchImportRow row)
    {
        var loc = string.IsNullOrWhiteSpace(row.SheetName)
            ? $"dòng {row.ExcelRow}"
            : $"{row.SheetName} dòng {row.ExcelRow}" + (string.IsNullOrWhiteSpace(row.BlockLabel) ? "" : $" {row.BlockLabel}");
        return loc;
    }

    private sealed record TripKey(DateTime Day, int VehicleId, int RouteId, int CustomerId);

    private sealed record ExistingTrip(
        DateTime PickupAt, int VehicleId, int RouteId, int CustomerId, string Code);

    private sealed record ImportContext(
        List<Customer> Customers,
        List<Location> Locations,
        List<Route> Routes,
        List<CustomerAlias> CustomerAliases,
        List<LocationAlias> LocationAliases,
        List<RouteAlias> RouteAliases,
        List<Vehicle> Vehicles,
        List<VehicleAlias> VehicleAliases,
        List<Driver> Drivers,
        List<PaymentMethod> Payments,
        List<VehicleType> VehicleTypes,
        List<ExistingTrip> Existing);
}
