using Hma.Desktop.Wpf.Infrastructure.Session;

namespace Hma.Desktop.Wpf.Tests;

public class UiOperationGateTests
{
    [Fact]
    public async Task Generic_run_returns_action_result_without_recursing()
    {
        var calls = 0;

        using var gate = new UiOperationGate();

        var result = await gate.RunAsync(async () =>
        {
            calls++;
            await Task.Yield();
            return 42;
        });

        Assert.Equal(42, result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Nested_operations_are_reentrant_on_the_same_async_flow()
    {
        using var gate = new UiOperationGate();
        var calls = 0;

        await gate.RunAsync(async () =>
        {
            calls++;
            await gate.RunAsync(() =>
            {
                calls++;
                return Task.CompletedTask;
            });
        });

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Concurrent_operations_are_serialized()
    {
        using var gate = new UiOperationGate();
        var running = 0;
        var maximum = 0;

        async Task Work()
        {
            await gate.RunAsync(async () =>
            {
                var current = Interlocked.Increment(ref running);
                maximum = Math.Max(maximum, current);
                await Task.Delay(20);
                Interlocked.Decrement(ref running);
            });
        }

        await Task.WhenAll(Work(), Work(), Work());

        Assert.Equal(1, maximum);
    }
}
