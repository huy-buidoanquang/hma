using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class VehiclePlateRulesTests
{
    [Fact]
    public void Canonicalize_accepts_hyphen_compact_and_lowercase()
    {
        Assert.Equal("29C-23456", VehiclePlateRules.Canonicalize("29C-23456"));
        Assert.Equal("29C-23456", VehiclePlateRules.Canonicalize("29C23456"));
        Assert.Equal("29C-23456", VehiclePlateRules.Canonicalize("29c23456"));
        Assert.Equal("30H-4567", VehiclePlateRules.Canonicalize("30h-4567"));
        Assert.Equal("29C-23456", VehiclePlateRules.Canonicalize(" 29C - 23456 "));
    }

    [Fact]
    public void Dictionary_forms_are_compact_and_lowercase_letter()
    {
        var forms = VehiclePlateRules.DictionaryForms("29C-23456");
        Assert.Contains("29C23456", forms);
        Assert.Contains("29c23456", forms);
    }

    [Fact]
    public void Rejects_wrong_shape()
    {
        Assert.False(VehiclePlateRules.TryCanonicalize("29CC-23456", out _));
        Assert.False(VehiclePlateRules.TryCanonicalize("29C-234", out _));
        Assert.False(VehiclePlateRules.TryCanonicalize("", out _));
    }
}
