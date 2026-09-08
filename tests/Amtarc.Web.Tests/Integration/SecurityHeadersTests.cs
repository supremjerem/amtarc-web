using System.Net;

namespace Amtarc.Web.Tests.Integration;

/// <summary>
/// The headers every response carries, and the one header that must not be there: with the API
/// folded into the app there are no cross-origin callers left to allow.
/// </summary>
[Collection(AmtarcWebCollection.Name)]
public sealed class SecurityHeadersTests(AmtarcWebFactory factory)
{
    private readonly AmtarcWebFactory _factory = factory;

    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Cross-Origin-Opener-Policy", "same-origin")]
    public async Task PublicPage_CarriesTheHeader(string name, string expected)
    {
        var response = await _factory.CreateClient().GetAsync("/");

        response.Headers.GetValues(name).Should().ContainSingle().Which.Should().Be(expected);
    }

    [Fact]
    public async Task PublicPage_CarriesAContentSecurityPolicyWithNoInlineEscapeHatch()
    {
        var response = await _factory.CreateClient().GetAsync("/");

        var csp = response.Headers.GetValues("Content-Security-Policy").Single();

        csp.Should().Contain("default-src 'self'");
        csp.Should().Contain("script-src 'self'");
        csp.Should().Contain("style-src 'self'");
        csp.Should().Contain("frame-ancestors 'none'");
        csp.Should().Contain("object-src 'none'");

        // The whole point: the ported markup carries no inline styles or scripts, so the policy
        // never has to open that door. If someone reintroduces one, this is what should stop them.
        csp.Should().NotContain("unsafe-inline");
        csp.Should().NotContain("unsafe-eval");
    }

    [Fact]
    public async Task RenderedPage_HasNoInlineStyleOrScript_SoTheStrictPolicyActuallyHolds()
    {
        var html = await _factory.CreateClient().GetStringAsync("/");

        html.Should().NotContain("style=\"", "an inline style attribute would be blocked by the CSP");
        html.Should().NotContain("<script>", "an inline script would be blocked by the CSP");
    }

    [Fact]
    public async Task AdminPage_CarriesTheHeadersToo()
    {
        var response = await LoginClient.CreateClient(_factory).GetAsync("/admin/login");

        response.Headers.Contains("Content-Security-Policy").Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
    }

    [Fact]
    public async Task StaticAsset_CarriesTheHeadersToo()
    {
        var response = await _factory.CreateClient().GetAsync("/css/site.css");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("Content-Security-Policy").Should().BeTrue();
    }

    [Fact]
    public async Task Response_EmitsNoCorsHeaders()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Origin", "https://elsewhere.example");

        var response = await _factory.CreateClient().SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
