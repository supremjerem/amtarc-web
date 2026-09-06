using System.Net;

namespace Amtarc.Web.Tests.Integration;

/// <summary>
/// Guards the design-system wiring: the compiled Tailwind CSS and the bundled TypeScript are
/// referenced by the layout and actually served, and the self-hosted fonts resolve. A broken
/// asset build otherwise fails silently — the page still renders, just unstyled.
/// </summary>
[Collection(AmtarcWebCollection.Name)]
public sealed class PublicLayoutTests(AmtarcWebFactory factory)
{
    private readonly AmtarcWebFactory _factory = factory;

    [Fact]
    public async Task HomePage_ReferencesTheCompiledStylesheetAndScript()
    {
        var html = await _factory.CreateClient().GetStringAsync("/");

        html.Should().Contain("/css/site.css");
        html.Should().Contain("/js/site.js");
    }

    [Fact]
    public async Task HomePage_DeclaresFrenchAndTheSiteTitle()
    {
        var html = await _factory.CreateClient().GetStringAsync("/");

        html.Should().Contain("<html lang=\"fr\"");
        html.Should().Contain("AMTARC — Club de tir de Chapas, Meauzac");
    }

    [Theory]
    [InlineData("/css/site.css")]
    [InlineData("/js/site.js")]
    [InlineData("/fonts/archivo-latin.woff2")]
    [InlineData("/fonts/instrument-sans-latin.woff2")]
    [InlineData("/fonts/martian-mono-latin.woff2")]
    [InlineData("/img/panther.png")]
    [InlineData("/img/amtarc-logo.png")]
    public async Task StaticAsset_IsServed(string path)
    {
        var response = await _factory.CreateClient().GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentLength.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CompiledStylesheet_ContainsTheDesignSystemTokens()
    {
        var css = await _factory.CreateClient().GetStringAsync("/css/site.css");

        // The three type roles and the signature width axis.
        css.Should().Contain("Archivo");
        css.Should().Contain("wdth");
        // Theme tokens that only exist because globals.css was ported intact.
        css.Should().Contain("bg-gold-gradient");
        css.Should().Contain("data-figure");
    }

    [Fact]
    public async Task BundledScript_ContainsTheScrollEffects()
    {
        var js = await _factory.CreateClient().GetStringAsync("/js/site.js");

        js.Should().Contain("IntersectionObserver");
        js.Should().Contain("display-compressed");
        js.Should().Contain("nav-scrolled");
    }
}
