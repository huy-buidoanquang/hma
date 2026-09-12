using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class TaxCodeRulesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0304123456")]
    [InlineData("0304123456-001")]
    [InlineData("0304123456001")]
    public void Accepts_empty_or_vn_tax_codes(string? taxCode) => Assert.True(TaxCodeRules.IsValid(taxCode));

    [Theory]
    [InlineData("que")]
    [InlineData("123")]
    [InlineData("030412345")]
    [InlineData("0304123456-01")]
    public void Rejects_invalid(string taxCode) => Assert.False(TaxCodeRules.IsValid(taxCode));
}
