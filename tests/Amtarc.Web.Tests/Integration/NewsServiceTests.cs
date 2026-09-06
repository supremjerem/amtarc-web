using Amtarc.Web.Data;
using Amtarc.Web.Domain;
using Amtarc.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amtarc.Web.Tests.Integration;

[Collection(AmtarcWebCollection.Name)]
public sealed class NewsServiceTests(AmtarcWebFactory factory) : IAsyncLifetime
{
    private readonly AmtarcWebFactory _factory = factory;

    public async Task InitializeAsync()
    {
        await using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AmtarcDbContext>();
        await db.News.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task FindPublishedAsync_ReturnsOnlyPublished_NewestFirst()
    {
        await SeedAsync(
            NewsItem("draft", published: false, publishedAt: Days(-1)),
            NewsItem("older", publishedAt: Days(-10)),
            NewsItem("newer", publishedAt: Days(-2)));

        var service = Resolve();
        var result = await service.FindPublishedAsync();

        result.Select(n => n.Slug).Should().Equal("newer", "older");
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_ReturnsBaseSlug_WhenFree()
    {
        var service = Resolve();

        var slug = await service.GenerateUniqueSlugAsync("Bourse aux armes");

        slug.Should().Be("bourse-aux-armes");
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_AppendsIncrementingSuffix_OnCollision()
    {
        await SeedAsync(
            NewsItem("bourse-aux-armes"),
            NewsItem("bourse-aux-armes-2"));

        var service = Resolve();

        var slug = await service.GenerateUniqueSlugAsync("Bourse aux armes");

        slug.Should().Be("bourse-aux-armes-3");
    }

    private INewsService Resolve()
    {
        var scope = _factory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<INewsService>();
    }

    private async Task SeedAsync(params News[] items)
    {
        await using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AmtarcDbContext>();
        db.News.AddRange(items);
        await db.SaveChangesAsync();
    }

    private static DateTime Days(int offset) =>
        DateTime.SpecifyKind(DateTime.UtcNow.AddDays(offset), DateTimeKind.Unspecified);

    private static News NewsItem(string slug, bool published = true, DateTime? publishedAt = null) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Slug = slug,
        Title = slug.Replace('-', ' '),
        Category = NewsCategory.Evenement,
        Body = "Corps de l'actualité.",
        Published = published,
        PublishedAt = publishedAt ?? Days(0),
    };
}
