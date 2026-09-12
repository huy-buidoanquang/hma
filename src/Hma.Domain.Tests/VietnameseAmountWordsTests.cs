using Hma.Domain.Formatting;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class VietnameseAmountWordsTests
{
    [Theory]
    [InlineData(0, "không đồng")]
    [InlineData(1, "một đồng")]
    [InlineData(15, "mười lăm đồng")]
    [InlineData(1_000_000, "một triệu đồng")]
    public void ToWords_matches_invoice_style(decimal amount, string expected) =>
        Assert.Equal(expected, VietnameseAmountWords.ToWords(amount));
}
