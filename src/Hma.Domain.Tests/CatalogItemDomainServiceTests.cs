using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class CatalogItemDomainServiceTests
{
    [Fact]
    public void Rejects_blank_code_or_name()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CatalogItemDomainService.EnsureCanSave(" ", "Hà Nội", "thành phố"));
        Assert.Contains("bắt buộc", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Accepts_code_and_name()
    {
        CatalogItemDomainService.EnsureCanSave("HN", "Hà Nội", "thành phố");
    }
}
