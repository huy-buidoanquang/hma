namespace Hma.Domain.Rules;

public static class CatalogItemRules
{
    public static void EnsureCanSave(string? code, string? name, string entityLabel)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException($"Mã và tên {entityLabel} là bắt buộc.");
    }
}
