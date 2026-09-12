using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class CatalogItemRulesTests
{
    [Fact]
    public void Rejects_blank_code_or_name()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CatalogItemRules.EnsureCanSave(" ", "Hà Nội", "thành phố"));
        Assert.Contains("bắt buộc", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Accepts_code_and_name()
    {
        CatalogItemRules.EnsureCanSave("HN", "Hà Nội", "thành phố");
    }
}
