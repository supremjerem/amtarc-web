using Amtarc.Web.Content;
using Amtarc.Web.Domain;
using Amtarc.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Amtarc.Web.Tests.Integration;

/// <summary>
/// Caching for the public page, and the eviction that replaces the previous architecture's
/// revalidation webhook. Runs on its own host because the rest of the suite deliberately turns
/// caching off — here it has to be on for the test to mean anything.
/// </summary>
[Collection(AmtarcWebCollection.Name)]
public sealed class OutputCacheTests(AmtarcWebFactory factory) : IAsyncLifetime
{
    private readonly AmtarcWebFactory _factory = factory;
    private WebApplicationFactory<Program> _cached = null!;

    public Task InitializeAsync()
    {
        _cached = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Security:PublicCacheSeconds", "300"));

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var scope = _factory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISiteContentService>().ResetAsync(SectionKey.Hero);

        _cached.Dispose();
    }

    [Fact]
    public async Task PublicPage_IsServedFromTheCache_UntilSomethingIsWritten()
    {
        var client = _cached.CreateClient();
        await client.GetStringAsync("/");

        // Written straight to the database, so nothing evicts: the cached copy must still stand.
        var title = $"Contourne le cache {Guid.NewGuid():N}";
        await InsertNewsDirectlyAsync(title);

        (await client.GetStringAsync("/")).Should().NotContain(title);
    }

    [Fact]
    public async Task WritingANewsItem_EvictsTheCachedPage()
    {
        var client = _cached.CreateClient();
        await client.GetStringAsync("/");

        // Through the service this time, which is where eviction lives. It has to be this host's
        // service: the output cache is in-process, so a sibling host evicts a different cache.
        var title = $"Passe par le service {Guid.NewGuid():N}";
        await using (var scope = _cached.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<INewsService>().CreateAsync(new NewsInput
            {
                Title = title,
                Category = NewsCategory.Travaux,
                Body = "Le cache doit être vidé par cette écriture.",
                Published = true,
            });
        }

        (await client.GetStringAsync("/")).Should().Contain(title);
    }

    [Fact]
    public async Task SavingASection_EvictsTheCachedPage()
    {
        var client = _cached.CreateClient();
        await client.GetStringAsync("/");

        var badge = $"Badge {Guid.NewGuid().ToString("N")[..8]}";
        await using (var scope = _cached.Services.CreateAsyncScope())
        {
            var content = scope.ServiceProvider.GetRequiredService<ISiteContentService>();
            var hero = await content.GetSectionAsync(SectionKey.Hero);
            hero["badge"] = badge;
            await content.UpsertAsync(SectionKey.Hero, hero);
        }

        (await client.GetStringAsync("/")).Should().Contain(badge);
    }

    [Fact]
    public async Task SignedInAdmin_IsNeverServedACachedPage()
    {
        var anonymous = _cached.CreateClient();
        await anonymous.GetStringAsync("/");

        var admin = await LoginClient.CreateAuthenticatedClientAsync(_cached);

        // Bypasses eviction on purpose: an admin looking at the site must see the current state,
        // not a copy cached for visitors.
        var title = $"Vue admin {Guid.NewGuid():N}";
        await InsertNewsDirectlyAsync(title);

        (await admin.GetStringAsync("/")).Should().Contain(title);
    }

    private async Task InsertNewsDirectlyAsync(string title)
    {
        await using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Web.Data.AmtarcDbContext>();

        db.News.Add(new News
        {
            Id = Guid.NewGuid().ToString(),
            Slug = $"cache-{Guid.NewGuid():N}",
            Title = title,
            Category = NewsCategory.Concours,
            Body = "Écrit directement en base, sans passer par le service.",
            Published = true,
            PublishedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
    }
}
