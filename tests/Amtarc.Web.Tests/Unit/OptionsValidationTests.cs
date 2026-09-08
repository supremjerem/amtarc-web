using System.ComponentModel.DataAnnotations;
using Amtarc.Web.Options;

namespace Amtarc.Web.Tests.Unit;

/// <summary>
/// The rules that make the app refuse to boot. A deployment that kept the example values is the
/// realistic way this site ends up with a back-office anyone can log into, so these are the checks
/// that cannot be skipped by not reading the README. Ported from the previous site's Zod schema.
/// </summary>
public sealed class OptionsValidationTests
{
    private static IReadOnlyList<string> Validate(object options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        return [.. results.Select(r => r.ErrorMessage ?? string.Empty)];
    }

    private static AdminOptions ValidAdmin() => new()
    {
        Email = "bureau@amtarc.fr",
        Password = "un-mot-de-passe-assez-long",
    };

    [Fact]
    public void AdminOptions_AreValid_WhenEmailAndPasswordAreReal()
    {
        Validate(ValidAdmin()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("rootz3r")]
    [InlineData("not-an-email")]
    public void AdminOptions_RejectAnEmailThatIsNotOne(string email)
    {
        // The live site once failed to boot on exactly this: an admin username where an address
        // was expected.
        var options = ValidAdmin();
        options.Email = email;

        Validate(options).Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("court")]
    [InlineData("onze-carac")]
    public void AdminOptions_RejectAPasswordUnderTwelveCharacters(string password)
    {
        var options = ValidAdmin();
        options.Password = password;

        Validate(options).Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("change-me")]
    [InlineData("changeme")]
    [InlineData("change_me")]
    [InlineData("CHANGE-ME")]
    [InlineData("password")]
    [InlineData("secret")]
    [InlineData("admin")]
    public void AdminOptions_RejectAPlaceholderPassword(string password)
    {
        var options = ValidAdmin();
        options.Password = password;

        Validate(options).Should().NotBeEmpty();
    }

    [Fact]
    public void AdminOptions_RejectAPlaceholderEvenWhenItIsLongEnough()
    {
        // "change-me" is short enough to be caught by the length rule; pad it so only the
        // placeholder rule can reject it.
        var options = ValidAdmin();
        options.Password = "  change_me  ";

        Validate(options).Should().ContainMatch("*placeholder*");
    }

    [Fact]
    public void UploadOptions_AreValid_ByDefault()
    {
        Validate(new UploadOptions()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(200L * 1024 * 1024)]
    public void UploadOptions_RejectAnImplausibleSizeLimit(long maxBytes)
    {
        Validate(new UploadOptions { MaxBytes = maxBytes }).Should().NotBeEmpty();
    }

    [Fact]
    public void UploadOptions_RejectAnEmptyDirectory()
    {
        Validate(new UploadOptions { Directory = string.Empty }).Should().NotBeEmpty();
    }

    [Fact]
    public void SecurityOptions_AreValid_ByDefault()
    {
        Validate(new SecurityOptions()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    public void SecurityOptions_RejectAnImplausibleProxyHopCount(int hops)
    {
        // Trusting one hop too many lets a caller spoof the client address the throttle keys on.
        Validate(new SecurityOptions { TrustProxyHops = hops }).Should().NotBeEmpty();
    }

    [Fact]
    public void SecurityOptions_RejectALimitOfZero()
    {
        Validate(new SecurityOptions { RequestLimit = 0 }).Should().NotBeEmpty();
        Validate(new SecurityOptions { LoginAttemptLimit = 0 }).Should().NotBeEmpty();
    }

    [Fact]
    public void SecurityOptions_AllowCachingToBeTurnedOff()
    {
        Validate(new SecurityOptions { PublicCacheSeconds = 0 }).Should().BeEmpty();
    }
}
