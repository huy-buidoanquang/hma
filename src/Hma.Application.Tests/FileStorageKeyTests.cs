using Hma.Application.Services;

namespace Hma.Application.Tests;

public class FileStorageKeyTests
{
    [Fact]
    public void ForDispatchDocument_uses_relative_key()
    {
        var key = FileStorageKey.ForDispatchDocument(123, @"C:\inbox\bbgh.pdf");
        Assert.StartsWith("000123/", key, StringComparison.Ordinal);
        Assert.EndsWith("-bbgh.pdf", key, StringComparison.Ordinal);
        Assert.DoesNotContain(":", key, StringComparison.Ordinal);
        Assert.DoesNotContain("\\", key, StringComparison.Ordinal);
    }

    [Fact]
    public void ForDispatchDocument_never_reuses_a_key()
    {
        var first = FileStorageKey.ForDispatchDocument(123, "bbgh.pdf");
        var second = FileStorageKey.ForDispatchDocument(123, "bbgh.pdf");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Normalize_rejects_parent_segments() =>
        Assert.Throws<InvalidOperationException>(() => FileStorageKey.Normalize("000123/../secret.pdf"));

    [Fact]
    public void SanitizeFileName_replaces_invalid_characters() =>
        Assert.Equal("bbgh_.pdf", FileStorageKey.SanitizeFileName("bbgh*.pdf"));
}
