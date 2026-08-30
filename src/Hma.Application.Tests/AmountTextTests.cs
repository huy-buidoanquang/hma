using Hma.Application.Services;

namespace Hma.Application.Tests;

public class AmountTextTests
{
    [Fact]
    public void From_delegates_to_vietnamese_words() =>
        Assert.Equal("một trăm đồng", AmountText.From(100));
}
