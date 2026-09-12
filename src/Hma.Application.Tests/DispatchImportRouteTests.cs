using Hma.Application.Features.Dispatching.Import;

namespace Hma.Application.Tests;

public class DispatchImportRouteTests
{
    [Fact]
    public void Idle_and_date_caption_are_not_trips()
    {
        Assert.False(DispatchImportRoute.IsIdle(" "));
        Assert.False(DispatchImportRoute.IsIdle(""));
        Assert.True(DispatchImportRoute.IsIdle("X"));
        Assert.True(DispatchImportRoute.IsIdle("lưu"));
        Assert.True(DispatchImportRoute.IsDateCaption("Ngày 11 tháng 08 năm 2026"));
        Assert.False(DispatchImportRoute.IsIdle("nb - hp"));
    }

    [Fact]
    public void Split_common_ops_separators()
    {
        Assert.True(DispatchImportRoute.TrySplit("Qchau - nb", out var a, out var b));
        Assert.Equal("Qchau", a);
        Assert.Equal("nb", b);
        Assert.True(DispatchImportRoute.TrySplit("QV =- nb", out a, out b));
        Assert.Equal("QV", a);
        Assert.Equal("nb", b);
        Assert.True(DispatchImportRoute.TrySplit("Nb- võ cường", out a, out b));
        Assert.Equal("Nb", a);
        Assert.Equal("võ cường", b);
        Assert.False(DispatchImportRoute.TrySplit("chuyển kho nb", out _, out _));
    }

    [Fact]
    public void SplitStops_handles_three_points()
    {
        Assert.True(DispatchImportRoute.TrySplitStops("nb - hp - hl", out var stops));
        Assert.Equal(["nb", "hp", "hl"], stops);
        Assert.False(DispatchImportRoute.TrySplit("nb - hp - hl", out _, out _));
    }
}
