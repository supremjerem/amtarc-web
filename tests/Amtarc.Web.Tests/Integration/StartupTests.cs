using System.Net;
using System.Net.Http.Json;
using Amtarc.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amtarc.Web.Tests.Integration;

[Collection(AmtarcWebCollection.Name)]
public sealed class StartupTests(AmtarcWebFactory factory)
{
    private readonly AmtarcWebFactory _factory = factory;

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

    [Fact]
    public async Task Startup_AppliesMigrations()
    {
        await using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AmtarcDbContext>();

        var applied = await db.Database.GetAppliedMigrationsAsync();
        var pending = await db.Database.GetPendingMigrationsAsync();

        applied.Should().Contain(m => m.EndsWith("_Initial", StringComparison.Ordinal));
        pending.Should().BeEmpty();
    }

    [Fact]
    public async Task Startup_SeedsTheAdminAccount()
    {
        await using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AmtarcDbContext>();

        var admin = await db.Admins.SingleOrDefaultAsync(a => a.Email == AmtarcWebFactory.AdminEmail);

        admin.Should().NotBeNull();
        admin!.PasswordHash.Should().NotBeNullOrWhiteSpace();
        BCrypt.Net.BCrypt.Verify(AmtarcWebFactory.AdminPassword, admin.PasswordHash).Should().BeTrue();
    }

    private sealed record HealthResponse(string Status);
}
