using Hma.Application.Services;

namespace Hma.Application.Tests;

public class DispatchImportCatalogKeyTests
{
    [Fact]
    public void FoldKey_merges_d_stroke_spaces_and_marks()
    {
        Assert.Equal(DispatchImportCatalogKey.FoldKey("DONGJIN"), DispatchImportCatalogKey.FoldKey("ĐONGJIN"));
        Assert.Equal(DispatchImportCatalogKey.FoldKey("hai dang"), DispatchImportCatalogKey.FoldKey("hải đăng"));
        Assert.Equal(DispatchImportCatalogKey.FoldKey("mai vtri"), DispatchImportCatalogKey.FoldKey("maiv tri"));
        Assert.Equal(DispatchImportCatalogKey.FoldKey("sheng yang"), DispatchImportCatalogKey.FoldKey("SHENGYANG"));
        Assert.Equal(DispatchImportCatalogKey.FoldKey("D thuy"), DispatchImportCatalogKey.FoldKey("dthuy"));
        Assert.Equal(DispatchImportCatalogKey.FoldKey("N b"), DispatchImportCatalogKey.FoldKey("nb"));
        Assert.NotEqual(DispatchImportCatalogKey.FoldKey("SHEGYANG"), DispatchImportCatalogKey.FoldKey("SHENGYANG"));
        Assert.NotEqual(DispatchImportCatalogKey.FoldKey("dthy"), DispatchImportCatalogKey.FoldKey("dthuy"));
    }

    [Fact]
    public void CanonicalLocationToken_strips_ops_notes_not_place_names()
    {
        Assert.Equal("nb", DispatchImportCatalogKey.CanonicalLocationToken("nb kiểm"));
        Assert.Equal("nb", DispatchImportCatalogKey.CanonicalLocationToken("nb kiẻm"));
        Assert.Equal("nb", DispatchImportCatalogKey.CanonicalLocationToken("nb , vé"));
        Assert.Equal("nb", DispatchImportCatalogKey.CanonicalLocationToken("2 kho nb"));
        Assert.Equal("Nb", DispatchImportCatalogKey.CanonicalLocationToken("Nb 2 kho"));
        Assert.Equal("nb", DispatchImportCatalogKey.CanonicalLocationToken("nb 3 kho + chờ 200k"));
        Assert.Equal("nb", DispatchImportCatalogKey.CanonicalLocationToken("nb lưu đêm + 2 kho"));
        Assert.Equal("LGE", DispatchImportCatalogKey.CanonicalLocationToken(":LGE"));
        Assert.Equal("nb", DispatchImportCatalogKey.CanonicalLocationToken("nb\\"));
        Assert.Equal("LGD", DispatchImportCatalogKey.CanonicalLocationToken("LGD rung vé"));
        Assert.Equal("đình trám", DispatchImportCatalogKey.CanonicalLocationToken("kiểm đình trám"));
        Assert.Equal("BT", DispatchImportCatalogKey.CanonicalLocationToken("BT kiểm"));
        Assert.Equal("vsip bn", DispatchImportCatalogKey.CanonicalLocationToken("vsip bn lạnh, bốc"));
        Assert.Equal("hà nội giấy", DispatchImportCatalogKey.CanonicalLocationToken("hà nội giấy 2 điểm"));
        Assert.Equal("Tp nam định", DispatchImportCatalogKey.CanonicalLocationToken("Tp nam định, luật 100k"));
    }

    [Fact]
    public void CanonicalLocationToken_keeps_distinct_gates_and_real_names()
    {
        Assert.Equal("QV 2", DispatchImportCatalogKey.CanonicalLocationToken("QV 2"));
        Assert.Equal("QV 3", DispatchImportCatalogKey.CanonicalLocationToken("QV 3"));
        Assert.Equal("T long 2", DispatchImportCatalogKey.CanonicalLocationToken("T long 2"));
        Assert.Equal("BX 2", DispatchImportCatalogKey.CanonicalLocationToken("BX 2"));
        Assert.Equal("ĐV 4", DispatchImportCatalogKey.CanonicalLocationToken("ĐV 4"));
        Assert.Equal("v trung", DispatchImportCatalogKey.CanonicalLocationToken("v trung"));
        Assert.Equal("hà đong giấy", DispatchImportCatalogKey.CanonicalLocationToken("hà đong giấy"));
        Assert.Equal("nb nguyên", DispatchImportCatalogKey.CanonicalLocationToken("nb nguyên"));
        Assert.Equal("nb QĐ", DispatchImportCatalogKey.CanonicalLocationToken("nb QĐ lưu đêm"));
        Assert.Equal("sevt", DispatchImportCatalogKey.CanonicalLocationToken("sevt bốc 100k"));
        Assert.Equal("", DispatchImportCatalogKey.CanonicalLocationToken(" "));
    }
}
