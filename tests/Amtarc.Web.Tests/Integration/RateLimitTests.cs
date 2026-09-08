using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Amtarc.Web.Tests.Integration;

/// <summary>
/// The global throttle. It runs on its own host with a tiny budget — hammering the shared one
/// would starve every other test in the collection.
/// </summary>
[Collection(AmtarcWebCollection.Name)]
public sealed class RateLimitTests(AmtarcWebFactory factory)
{
    private readonly AmtarcWebFactory _factory = factory;

    private WebApplicationFactory<Program> Throttled(int limit) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Security:RequestLimit", limit.ToString(CultureInfo.InvariantCulture));
            builder.UseSetting("Security:RequestWindowSeconds", "60");
        });

    [Fact]
    public async Task Burst_IsAnsweredWith429AndARetryAfterHeader()
    {
        const int limit = 5;
        using var throttled = Throttled(limit);
        var client = LoginClient.CreateClient(throttled);

        for (var i = 0; i < limit; i++)
        {
            var allowed = await client.GetAsync("/healthz");
            allowed.StatusCode.Should().Be(HttpStatusCode.OK, "request {0} is within the budget", i + 1);
        }

        var blocked = await client.GetAsync("/healthz");

        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        blocked.Headers.RetryAfter.Should().NotBeNull("a throttled caller needs to know when to come back");
    }

    [Fact]
    public async Task StaticAssets_DoNotConsumeTheBudget()
    {
        const int limit = 3;
        using var throttled = Throttled(limit);
        var client = LoginClient.CreateClient(throttled);

        // One page view pulls a stylesheet, a script, fonts and images. Counting those against a
        // per-visitor budget meant for pages would throttle ordinary browsing after a click or two.
        for (var i = 0; i < limit * 4; i++)
        {
            var asset = await client.GetAsync("/css/site.css");
            asset.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        (await client.GetAsync("/healthz")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
