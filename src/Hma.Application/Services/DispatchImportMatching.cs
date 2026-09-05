using System.Globalization;
using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Application.Services;

public static class DispatchImportMatching
{
    public static Customer? MatchCustomer(IEnumerable<Customer> items, string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var key = code.Trim();
        return items.FirstOrDefault(c => c.Code.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public static Customer? MatchCustomer(
        IEnumerable<Customer> items, IEnumerable<CustomerAlias> aliases, string? value)
    {
        var customer = MatchCustomer(items, value);
        if (customer is not null) return customer;
        if (string.IsNullOrWhiteSpace(value)) return null;
        var alias = aliases.FirstOrDefault(a => AliasText.EqualsNormalized(a.Alias, value));
        if (alias is null) return null;
        return alias.Customer ?? items.FirstOrDefault(c => c.Id == alias.CustomerId);
    }

    public static Location? MatchLocation(IEnumerable<Location> items, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var key = value.Trim();
        return items.FirstOrDefault(c => c.Code.Equals(key, StringComparison.OrdinalIgnoreCase))
               ?? items.FirstOrDefault(c => c.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public static Location? MatchLocation(
        IEnumerable<Location> locations,
        IEnumerable<LocationAlias> aliases,
        string? value,
        LocationMatchRole role)
    {
        var location = MatchLocation(locations, value);
        if (location is not null) return location;
        if (string.IsNullOrWhiteSpace(value)) return null;
        var alias = aliases.FirstOrDefault(a =>
            AliasText.EqualsNormalized(a.Alias, value) && RoleApplies(a.Kind, role));
        if (alias is null) return null;
        return alias.Location ?? locations.FirstOrDefault(c => c.Id == alias.LocationId);
    }

    public static Route? MatchRoute(
        IEnumerable<Route> routes, IEnumerable<RouteAlias> aliases, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var alias = aliases.FirstOrDefault(a => AliasText.EqualsNormalized(a.Alias, value));
        if (alias is not null)
            return alias.Route ?? routes.FirstOrDefault(r => r.Id == alias.RouteId);
        var key = value.Trim();
        return routes.FirstOrDefault(r => r.Code.Equals(key, StringComparison.OrdinalIgnoreCase))
               ?? routes.FirstOrDefault(r => r.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public static Route? MatchRouteByFingerprint(IEnumerable<Route> routes, IReadOnlyList<int> locationIds)
    {
        if (locationIds.Count < 2) return null;
        var fingerprint = RouteFingerprint.From(locationIds);
        return routes.FirstOrDefault(r => r.Fingerprint == fingerprint);
    }

    public static (Route? Route, List<string> Errors) ResolveRoute(
        string? routeText,
        string? pickupText,
        string? deliveryText,
        IEnumerable<Route> routes,
        IEnumerable<RouteAlias> routeAliases,
        IEnumerable<Location> locations,
        IEnumerable<LocationAlias> locationAliases)
    {
        var routeList = routes as IList<Route> ?? routes.ToList();
        var locationList = locations as IList<Location> ?? locations.ToList();
        var locationAliasList = locationAliases as IList<LocationAlias> ?? locationAliases.ToList();
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(routeText))
        {
            var byAlias = MatchRoute(routeList, routeAliases, routeText);
            if (byAlias is not null)
                return (byAlias, errors);

            if (!DispatchImportRoute.TrySplitStops(routeText, out var tokens))
            {
                errors.Add($"Không khớp tuyến «{routeText.Trim()}». Thêm bí danh tuyến hoặc ghi dạng A - B - C.");
                return (null, errors);
            }

            return ResolveFromTokens(tokens, routeList, locationList, locationAliasList);
        }

        if (!string.IsNullOrWhiteSpace(pickupText) || !string.IsNullOrWhiteSpace(deliveryText))
        {
            if (string.IsNullOrWhiteSpace(pickupText) || string.IsNullOrWhiteSpace(deliveryText))
            {
                errors.Add("Thiếu điểm đi hoặc điểm đến.");
                return (null, errors);
            }

            return ResolveFromTokens([pickupText.Trim(), deliveryText.Trim()], routeList, locationList, locationAliasList);
        }

        errors.Add("Thiếu tuyến đường.");
        return (null, errors);
    }

    private static (Route? Route, List<string> Errors) ResolveFromTokens(
        IReadOnlyList<string> tokens,
        IList<Route> routes,
        IList<Location> locations,
        IList<LocationAlias> aliases)
    {
        var errors = new List<string>();
        var ids = new List<int>();
        var names = new List<string>();
        for (var i = 0; i < tokens.Count; i++)
        {
            var role = i == 0
                ? LocationMatchRole.Pickup
                : i == tokens.Count - 1
                    ? LocationMatchRole.Delivery
                    : LocationMatchRole.Via;
            var location = MatchLocation(locations, aliases, tokens[i], role);
            if (location is null)
            {
                errors.Add($"Không tìm thấy điểm «{tokens[i]}». Thêm bí danh tại Cấu hình → Từ điển điểm.");
                continue;
            }

            ids.Add(location.Id);
            names.Add(location.Name);
        }

        if (errors.Count > 0)
            return (null, errors);

        var route = MatchRouteByFingerprint(routes, ids);
        if (route is null)
        {
            var label = RouteFingerprint.FormatLabel(names);
            errors.Add($"Chưa có tuyến {label} trong danh mục tuyến.");
            return (null, errors);
        }

        return (route, errors);
    }

    private static bool RoleApplies(LocationAliasKind kind, LocationMatchRole role) => role switch
    {
        LocationMatchRole.Pickup => LocationAliasRules.AppliesToPickup(kind),
        LocationMatchRole.Delivery => LocationAliasRules.AppliesToDelivery(kind),
        _ => LocationAliasRules.AppliesToVia(kind)
    };

    public static Vehicle? MatchVehicle(IEnumerable<Vehicle> items, string? plate)
    {
        if (string.IsNullOrWhiteSpace(plate)) return null;
        var key = NormalizePlate(plate);
        return items.FirstOrDefault(v => NormalizePlate(v.PlateNumber) == key);
    }

    public static Vehicle? MatchVehicle(
        IEnumerable<Vehicle> items, IEnumerable<VehicleAlias> aliases, string? plate)
    {
        var vehicle = MatchVehicle(items, plate);
        if (vehicle is not null) return vehicle;
        if (string.IsNullOrWhiteSpace(plate)) return null;
        var key = NormalizePlate(plate);
        var alias = aliases.FirstOrDefault(a =>
            AliasText.EqualsNormalized(a.Alias, plate) || NormalizePlate(a.Alias) == key);
        if (alias is null) return null;
        return alias.Vehicle ?? items.FirstOrDefault(v => v.Id == alias.VehicleId);
    }

    public static string NormalizePlate(string? plate) =>
        (plate ?? "").Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal)
            .ToUpperInvariant();

    public static Driver? MatchDriver(IEnumerable<Driver> items, string? name, int partnerId)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var key = name.Trim();
            return items.FirstOrDefault(d => d.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                   ?? items.FirstOrDefault(d => d.Code.Equals(key, StringComparison.OrdinalIgnoreCase));
        }
        return items.FirstOrDefault(d => d.PartnerId == partnerId);
    }

    public static PaymentMethod? MatchPayment(IEnumerable<PaymentMethod> items, string? value)
    {
        var list = items as IList<PaymentMethod> ?? items.ToList();
        if (string.IsNullOrWhiteSpace(value))
            return list.FirstOrDefault(p => p.Code == PaymentMethodCodes.Credit);
        var key = value.Trim();
        return list.FirstOrDefault(p => p.Code.Equals(key, StringComparison.OrdinalIgnoreCase)
                                        || p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public static DateTime ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Thiếu ngày chạy.");
        var text = value.Trim();
        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy HH:mm" };
        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact;
        if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var vi))
            return vi;
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var inv))
            return inv;
        throw new InvalidOperationException("Ngày chạy không hợp lệ (dd/MM/yyyy).");
    }

    public static decimal ParseMoney(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        if (!TryParseMoney(value, out var amount))
            throw new InvalidOperationException($"{label} không phải số.");
        MoneyRules.EnsureNonNegative(amount, label);
        return amount;
    }

    public static bool TryParseMoney(string? value, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var text = value.Trim().Replace(" ", "", StringComparison.Ordinal);
        var hasK = text.EndsWith("k", StringComparison.OrdinalIgnoreCase);
        if (hasK)
            text = text[..^1].Trim();
        if (!MoneyRules.TryParse(text, out amount))
            return false;
        if (hasK && CountThousandGroups(text) < 2)
            amount *= 1000;
        return true;
    }

    private static int CountThousandGroups(string text)
    {
        var t = text.Replace(",", ".");
        var n = 0;
        while (t.Length >= 4
               && t[^4] == '.'
               && char.IsDigit(t[^3]) && char.IsDigit(t[^2]) && char.IsDigit(t[^1]))
        {
            n++;
            t = t[..^4];
        }
        return n;
    }
}
