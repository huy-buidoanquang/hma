using System.Globalization;
using System.Text;
using Hma.Application.Services;
using Hma.Domain.Services;
using Hma.Reporting;

var repo = FindRepo();
var xlsx = Path.Combine(repo, "11_08_2026.xlsx");
if (!File.Exists(xlsx))
    throw new InvalidOperationException($"Missing {xlsx}");

using var stream = File.OpenRead(xlsx);
var rows = new DispatchImportParser().Parse(stream);
if (rows.Count == 0)
    throw new InvalidOperationException("Parser returned no rows.");

var customers = BuildCustomers(rows);
var drivers = BuildDrivers(rows);
var vehicles = BuildVehicles(rows);
var locations = BuildLocations(rows);
var locByKey = locations.ToDictionary(l => l.Key, StringComparer.Ordinal);
var routes = BuildRoutes(rows, locByKey);

var dest = Path.Combine(repo, "src", "Hma.Infrastructure.SqlServer", "OpsBoardCatalogData.cs");
File.WriteAllText(dest, Render(customers, drivers, vehicles, locations, routes), new UTF8Encoding(false));
Console.WriteLine($"Wrote {dest}");
Console.WriteLine($"rows={rows.Count} customers={customers.Count} drivers={drivers.Count} vehicles={vehicles.Count} locations={locations.Count} routes={routes.Count}");

static string FindRepo()
{
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "11_08_2026.xlsx"))
            && Directory.Exists(Path.Combine(dir.FullName, "src")))
            return dir.FullName;
    }

    throw new InvalidOperationException("Repo root not found.");
}

static List<string> BuildDrivers(IReadOnlyList<DispatchImportRow> rows)
{
    var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    foreach (var name in rows.Select(r => r.DriverName?.Trim() ?? "").Where(n => n.Length > 0))
    {
        if (!groups.TryGetValue(name, out var list))
            groups[name] = list = [];
        list.Add(name);
    }

    return groups.Values
        .Select(MostFrequent)
        .OrderBy(n => n, StringComparer.CurrentCulture)
        .ToList();
}

static List<Item> BuildCustomers(IReadOnlyList<DispatchImportRow> rows)
{
    var groups = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    foreach (var raw in rows.Select(r => AliasText.Normalize(r.CustomerCode)).Where(s => s.Length > 0))
    {
        var key = DispatchImportCatalogKey.FoldKey(raw);
        if (!groups.TryGetValue(key, out var list))
            groups[key] = list = [];
        list.Add(raw);
    }

    var items = new List<Item>();
    var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var list in groups.Values.OrderBy(g => CanonicalCustomer(g), StringComparer.OrdinalIgnoreCase))
    {
        var code = Unique(usedCodes, Truncate(CanonicalCustomer(list), 50));
        var aliases = list
            .Distinct(StringComparer.Ordinal)
            .Where(a => !AliasText.EqualsNormalized(a, code) && a.Length <= AliasText.MaxLength)
            .OrderBy(a => a, StringComparer.Ordinal)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        items.Add(new Item(code, code, aliases, ""));
    }

    return items;
}

static string CanonicalCustomer(List<string> spellings)
{
    var cleaned = spellings
        .Select(DispatchImportCatalogKey.TrimToken)
        .Where(s => s.Length > 0)
        .ToList();
    var source = cleaned.Count > 0 ? cleaned : spellings;
    var compact = source
        .Where(s => s.Length is > 0 and <= 20 && s.All(char.IsLetterOrDigit) && !s.Any(char.IsWhiteSpace))
        .ToList();
    var top = MostFrequent(compact.Count > 0 ? compact : source);
    if (top.All(char.IsLetterOrDigit) && !top.Any(char.IsWhiteSpace) && top.Length <= 20)
        return top.ToUpperInvariant();
    return top;
}

static List<VehicleItem> BuildVehicles(IReadOnlyList<DispatchImportRow> rows)
{
    var byPlate = new Dictionary<string, List<DispatchImportRow>>(StringComparer.Ordinal);
    foreach (var row in rows)
    {
        if (!VehiclePlateRules.TryCanonicalize(row.Plate, out var plate))
            continue;
        if (!byPlate.TryGetValue(plate, out var list))
            byPlate[plate] = list = [];
        list.Add(row);
    }

    return byPlate
        .OrderBy(p => p.Key, StringComparer.Ordinal)
        .Select(p => new VehicleItem(p.Key, MajorityType(p.Value)))
        .ToList();
}

static string? MajorityType(List<DispatchImportRow> trips)
{
    var votes = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var row in trips)
    {
        var code = TypeFromSheet(row.SheetName);
        if (code is null && string.Equals(row.SheetName, "3.5 - 5", StringComparison.Ordinal)
            && DispatchImportTonnage.TryParse(row.Notes, out var tons))
            code = TypeFromTons(tons);
        if (code is null)
            continue;
        votes[code] = votes.GetValueOrDefault(code) + 1;
    }

    return votes.Count == 0
        ? null
        : votes.OrderByDescending(v => v.Value).ThenBy(v => v.Key, StringComparer.Ordinal).First().Key;
}

static string? TypeFromSheet(string? sheet) => sheet switch
{
    "1.25" => "1.25T",
    "1.5" => "1.5T",
    "2.5" => "2.5T",
    "8" => "8T",
    "10" => "10T",
    _ => null
};

static string? TypeFromTons(decimal tons)
{
    if (tons == 1.25m) return "1.25T";
    if (tons == 1.5m) return "1.5T";
    if (tons == 2.5m) return "2.5T";
    if (tons == 3.5m) return "3.5T";
    if (tons == 5m) return "5T";
    if (tons == 8m) return "8T";
    if (tons == 10m) return "10T";
    if (tons == 15m) return "15T";
    return null;
}

static List<Item> BuildLocations(IReadOnlyList<DispatchImportRow> rows)
{
    var groups = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    foreach (var row in rows)
    {
        if (!DispatchImportRoute.TrySplitStops(row.Route, out var stops))
            continue;
        foreach (var token in stops)
        {
            var raw = AliasText.Normalize(token);
            if (raw.Length == 0) continue;
            var cleaned = DispatchImportCatalogKey.CanonicalLocationToken(raw);
            if (cleaned.Length == 0) continue;
            var key = DispatchImportCatalogKey.FoldKey(cleaned);
            if (!groups.TryGetValue(key, out var list))
                groups[key] = list = [];
            list.Add(raw);
        }
    }

    var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var items = new List<Item>();
    foreach (var (key, list) in groups.OrderBy(g => g.Key, StringComparer.Ordinal))
    {
        var name = MostFrequent(list.Select(DispatchImportCatalogKey.CanonicalLocationToken).ToList());
        var code = Unique(used, Truncate(name, 50));
        var aliases = list
            .Distinct(StringComparer.Ordinal)
            .Where(a => !AliasText.EqualsNormalized(a, code)
                        && !AliasText.EqualsNormalized(a, name)
                        && a.Length <= AliasText.MaxLength)
            .OrderBy(a => a, StringComparer.Ordinal)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        items.Add(new Item(code, name, aliases, key));
    }

    return items;
}

static List<RouteItem> BuildRoutes(IReadOnlyList<DispatchImportRow> rows, Dictionary<string, Item> locByKey)
{
    var groups = new Dictionary<string, RouteAcc>(StringComparer.Ordinal);
    foreach (var row in rows)
    {
        var alias = AliasText.Normalize(row.Route);
        if (alias.Length == 0 || !DispatchImportRoute.TrySplitStops(alias, out var stops))
            continue;
        var keys = new List<string>();
        var ok = true;
        foreach (var token in stops)
        {
            var key = DispatchImportCatalogKey.FoldKey(DispatchImportCatalogKey.CanonicalLocationToken(token));
            if (!locByKey.ContainsKey(key))
            {
                ok = false;
                break;
            }

            keys.Add(key);
        }

        if (!ok || keys.Count < 2)
            continue;

        var fp = string.Join('\u001f', keys);
        if (!groups.TryGetValue(fp, out var acc))
            groups[fp] = acc = new RouteAcc(keys.Select(k => locByKey[k].Code).ToArray(), []);
        if (alias.Length <= AliasText.MaxLength)
            acc.Aliases.Add(alias);
    }

    return groups.Values
        .Select(a => new RouteItem(
            a.StopCodes,
            a.Aliases.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.Ordinal).ToArray()))
        .OrderBy(r => string.Join('|', r.StopCodes), StringComparer.OrdinalIgnoreCase)
        .ToList();
}

static string MostFrequent(List<string> values) =>
    values.GroupBy(v => v, StringComparer.Ordinal)
        .OrderByDescending(g => g.Count())
        .ThenBy(g => g.Key, StringComparer.Ordinal)
        .First().Key;

static string Unique(HashSet<string> used, string candidate)
{
    var code = candidate;
    var n = 2;
    while (!used.Add(code))
    {
        var suffix = "-" + n.ToString(CultureInfo.InvariantCulture);
        var max = Math.Max(1, 50 - suffix.Length);
        code = Truncate(candidate, max) + suffix;
        n++;
    }

    return code;
}

static string Truncate(string value, int max) =>
    value.Length <= max ? value : value[..max].TrimEnd();

static string Render(
    List<Item> customers,
    List<string> drivers,
    List<VehicleItem> vehicles,
    List<Item> locations,
    List<RouteItem> routes)
{
    var sb = new StringBuilder();
    sb.AppendLine("namespace Hma.Infrastructure.SqlServer;");
    sb.AppendLine();
    sb.AppendLine("// Generated from 11_08_2026.xlsx (ops board). Do not parse Excel at runtime.");
    sb.AppendLine("public static class OpsBoardCatalogData");
    sb.AppendLine("{");
    sb.AppendLine("    public static readonly (string Code, string[] Aliases)[] Customers =");
    sb.AppendLine("    [");
    foreach (var c in customers)
        sb.AppendLine($"        ({Lit(c.Code)}, {StrArr(c.Aliases)}),");
    sb.AppendLine("    ];");
    sb.AppendLine();
    sb.AppendLine("    public static readonly string[] Drivers =");
    sb.AppendLine("    [");
    foreach (var d in drivers)
        sb.AppendLine($"        {Lit(d)},");
    sb.AppendLine("    ];");
    sb.AppendLine();
    sb.AppendLine("    public static readonly (string Plate, string? TypeCode)[] Vehicles =");
    sb.AppendLine("    [");
    foreach (var v in vehicles)
        sb.AppendLine($"        ({Lit(v.Plate)}, {(v.TypeCode is null ? "null" : Lit(v.TypeCode))}),");
    sb.AppendLine("    ];");
    sb.AppendLine();
    sb.AppendLine("    public static readonly (string Code, string Name, string[] Aliases)[] Locations =");
    sb.AppendLine("    [");
    foreach (var l in locations)
        sb.AppendLine($"        ({Lit(l.Code)}, {Lit(l.Name)}, {StrArr(l.Aliases)}),");
    sb.AppendLine("    ];");
    sb.AppendLine();
    sb.AppendLine("    public static readonly (string[] StopCodes, string[] Aliases)[] Routes =");
    sb.AppendLine("    [");
    foreach (var r in routes)
        sb.AppendLine($"        ({StrArr(r.StopCodes)}, {StrArr(r.Aliases)}),");
    sb.AppendLine("    ];");
    sb.AppendLine("}");
    return sb.ToString();
}

static string StrArr(string[] values) =>
    values.Length == 0 ? "[]" : "[" + string.Join(", ", values.Select(Lit)) + "]";

static string Lit(string value) =>
    "\"" + value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("\r", "", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
    + "\"";

sealed record Item(string Code, string Name, string[] Aliases, string Key);
sealed record VehicleItem(string Plate, string? TypeCode);
sealed record RouteItem(string[] StopCodes, string[] Aliases);
sealed class RouteAcc(string[] stopCodes, HashSet<string> aliases)
{
    public string[] StopCodes { get; } = stopCodes;
    public HashSet<string> Aliases { get; } = aliases;
}
