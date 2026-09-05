namespace Hma.Domain.Services;

public static class CatalogItemDomainService
{
    public static void EnsureCanSave(string? code, string? name, string entityLabel)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException($"Mã và tên {entityLabel} là bắt buộc.");
    }
}
