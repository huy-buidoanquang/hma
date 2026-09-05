using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class AliasTextTests
{
    [Fact]
    public void Normalize_trims_and_collapses_spaces()
    {
        Assert.Equal("cảng HP", AliasText.Normalize("  cảng   HP  "));
        Assert.Equal("", AliasText.Normalize("   "));
        Assert.Equal("", AliasText.Normalize(null));
    }

    [Fact]
    public void EqualsNormalized_is_case_insensitive()
    {
        Assert.True(AliasText.EqualsNormalized("nb", "NB"));
        Assert.True(AliasText.EqualsNormalized("  QV ", "qv"));
        Assert.False(AliasText.EqualsNormalized("nb", "hp"));
    }
}
