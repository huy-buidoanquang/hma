using Hma.Application.Services;

namespace Hma.Application.Tests;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _sut = new();

    [Fact]
    public void Hash_and_verify_round_trip()
    {
        var hash = _sut.Hash("Hma-Secure-2026!");

        Assert.True(_sut.Verify(hash, "Hma-Secure-2026!"));
        Assert.False(_sut.Verify(hash, "wrong"));
    }

    [Theory]
    [InlineData("RESET:legacy-password")]
    [InlineData("pbkdf2:not-base64:not-base64")]
    [InlineData("")]
    public void Verify_rejects_legacy_or_malformed_hash(string hash) =>
        Assert.False(_sut.Verify(hash, "legacy-password"));
}
