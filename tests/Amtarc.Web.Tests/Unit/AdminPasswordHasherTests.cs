using Amtarc.Web.Security;

namespace Amtarc.Web.Tests.Unit;

public sealed class AdminPasswordHasherTests
{
    private readonly AdminPasswordHasher _hasher = new();

    [Fact]
    public void Verify_ReturnsTrue_ForThePasswordThatWasHashed()
    {
        var hash = _hasher.Hash("un-mot-de-passe-solide");

        _hasher.Verify("un-mot-de-passe-solide", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_ReturnsFalse_ForAnyOtherPassword()
    {
        var hash = _hasher.Hash("un-mot-de-passe-solide");

        _hasher.Verify("un-mot-de-passe-solidE", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_UsesWorkFactor10_SoHashesRestoredFromProductionStillVerify()
    {
        // The previous site hashed with bcrypt cost 10; the prefix is part of that contract.
        _hasher.Hash("peu importe").Should().StartWith("$2a$10$");
    }

    [Fact]
    public void Hash_IsSalted_SoTheSamePasswordNeverProducesTheSameHashTwice()
    {
        _hasher.Hash("identique").Should().NotBe(_hasher.Hash("identique"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("pas-du-tout-un-hash")]
    [InlineData("$2a$10$trop-court")]
    public void Verify_FailsClosed_OnAHashItCannotParse(string hash)
    {
        // A corrupted or pre-bcrypt row must deny the sign-in, not throw a 500 out of the handler.
        _hasher.Verify("peu importe", hash).Should().BeFalse();
    }
}
