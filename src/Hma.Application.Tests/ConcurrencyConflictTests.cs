using Hma.Application.Services;

namespace Hma.Application.Tests;

public class ConcurrencyConflictTests
{
    [Fact]
    public void Throw_uses_vietnamese_reload_message()
    {
        var ex = Assert.Throws<InvalidOperationException>(ConcurrencyConflict.Throw);
        Assert.Equal(ConcurrencyConflict.Message, ex.Message);
        Assert.Contains("Tải lại", ex.Message, StringComparison.Ordinal);
    }
}
