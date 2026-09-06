using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Amtarc.Web.Tests;

public sealed class HealthCheckTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task Healthz_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/healthz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body!.Status.Should().Be("ok");
    }

    [Fact]
    public async Task HomePage_RendersClubName()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        html.Should().Contain("AMTARC");
    }

    private sealed record HealthResponse(string Status);
}
