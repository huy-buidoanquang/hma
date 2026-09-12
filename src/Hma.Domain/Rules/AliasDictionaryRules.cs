using Hma.Domain.Normalization;
using Hma.Domain.Entities;

namespace Hma.Domain.Rules;

public static class AliasDictionaryRules
{
    public static void EnsureUniqueKey(string alias, IEnumerable<string> otherKeys)
    {
        if (otherKeys.Any(existing => AliasText.EqualsNormalized(existing, alias)))
            throw new InvalidOperationException("Bí danh này đã tồn tại.");
    }

    public static void EnsureNotCatalog(string alias, IEnumerable<(string Code, string Name)> catalog, string catalogLabel)
    {
        foreach (var (code, name) in catalog)
        {
            if (AliasText.EqualsNormalized(code, alias) || AliasText.EqualsNormalized(name, alias))
                throw new InvalidOperationException(
                    $"Bí danh trùng mã hoặc tên {catalogLabel} — dùng catalog, không cần thêm bí danh.");
        }
    }

    public static IEnumerable<(string Code, string Name)> FromLocations(IEnumerable<Location> locations) =>
        locations.Select(l => (l.Code, l.Name));

    public static IEnumerable<(string Code, string Name)> FromRoutes(IEnumerable<Route> routes) =>
        routes.Select(r => (r.Code, r.Name));
}
