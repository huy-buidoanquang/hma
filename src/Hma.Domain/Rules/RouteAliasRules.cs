using Hma.Domain.Normalization;
namespace Hma.Domain.Rules;

public static class RouteAliasRules
{
    public static void EnsureCanSave(string? alias, int routeId)
    {
        var text = AliasText.Normalize(alias);
        if (text.Length == 0)
            throw new InvalidOperationException("Bí danh tuyến là bắt buộc.");
        if (text.Length > AliasText.MaxLength)
            throw new InvalidOperationException($"Bí danh tối đa {AliasText.MaxLength} ký tự.");
        if (routeId <= 0)
            throw new InvalidOperationException("Cần chọn tuyến đích.");
    }
}
