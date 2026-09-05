using Hma.Application.Services;

namespace Hma.Application.Tests;

public class DispatchImportTonnageTests
{
    [Fact]
    public void Parses_sheet_note_overrides()
    {
        Assert.True(DispatchImportTonnage.TryParse("1.25", out var t) && t == 1.25m);
        Assert.True(DispatchImportTonnage.TryParse("10t", out t) && t == 10m);
        Assert.True(DispatchImportTonnage.TryParse("125", out t) && t == 1.25m);
        Assert.True(DispatchImportTonnage.TryParse("2.5t", out t) && t == 2.5m);
        Assert.True(DispatchImportTonnage.TryParse("1.25.", out t) && t == 1.25m);
        Assert.False(DispatchImportTonnage.TryParse("chờ 100k", out _));
        Assert.False(DispatchImportTonnage.TryParse("lạnh", out _));
    }

    [Fact]
    public void Parses_ops_sheet_names_but_not_tonnage_range()
    {
        Assert.True(DispatchImportTonnage.TryParse("1.25", out var t) && t == 1.25m);
        Assert.True(DispatchImportTonnage.TryParse("1.5", out t) && t == 1.5m);
        Assert.True(DispatchImportTonnage.TryParse("2.5", out t) && t == 2.5m);
        Assert.True(DispatchImportTonnage.TryParse("8", out t) && t == 8m);
        Assert.True(DispatchImportTonnage.TryParse("10", out t) && t == 10m);
        Assert.False(DispatchImportTonnage.TryParse("3.5 - 5", out _));
    }
}
