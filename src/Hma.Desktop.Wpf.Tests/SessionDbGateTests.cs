using Hma.Desktop.Wpf.ViewModels;

namespace Hma.Desktop.Wpf.Tests;

public class SessionDbGateTests
{
    [Fact]
    public async Task Generic_run_returns_action_result_without_recursing()
    {
        var calls = 0;

        var result = await SessionDbGate.RunAsync(async () =>
        {
            calls++;
            await Task.Yield();
            return 42;
        });

        Assert.Equal(42, result);
        Assert.Equal(1, calls);
    }
}
