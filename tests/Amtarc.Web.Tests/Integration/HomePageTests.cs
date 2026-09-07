using System.Net;
using Amtarc.Web.Content;
using Amtarc.Web.Data;
using Amtarc.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amtarc.Web.Tests.Integration;

/// <summary>
/// The public site is a single page of eight anchored sections. These assert the page is
/// actually composed of all of them, that the copy is the club's, and that an admin's stored
/// override reaches the rendered output through the merge.
/// </summary>
[Collection(AmtarcWebCollection.Name)]
public sealed class HomePageTests(AmtarcWebFactory factory)
{
    private readonly AmtarcWebFactory _factory = factory;

    /// <summary>
    /// The rendered page, HTML-decoded. Razor escapes apostrophes to <c>&amp;#x27;</c>, and the
    /// club's copy is full of them ("HORAIRES D'OUVERTURE") — assert against the text a reader
    /// sees, not the encoder's output.
    /// </summary>
    private async Task<string> GetHomePageAsync()
    {
        var html = await _factory.CreateClient().GetStringAsync("/");
        return WebUtility.HtmlDecode(html);
    }

    [Theory]
    [InlineData("top")]
    [InlineData("annonces")]
    [InlineData("club")]
    [InlineData("disciplines")]
    [InlineData("tsv")]
    [InlineData("actus")]
    [InlineData("infos")]
    [InlineData("contact")]
    public async Task HomePage_RendersSection(string anchorId)
    {
        var html = await GetHomePageAsync();

        html.Should().Contain($"id=\"{anchorId}\"");
    }

    [Fact]
    public async Task HomePage_RendersTheClubCopy()
    {
        var html = await GetHomePageAsync();

        html.Should().Contain("La précision");                 // hero title
        html.Should().Contain("Certificat médical");           // announcements
        html.Should().Contain("Un stand complet");             // club intro
        html.Should().Contain("Tir Sportif de Vitesse");       // disciplines + tsv
        html.Should().Contain("Ce qui se passe à Chapas.");    // news heading
        html.Should().Contain("HORAIRES D'OUVERTURE");         // practical info
        html.Should().Contain("Venez tirer avec nous.");       // contact
        html.Should().Contain("contact@amtarc.fr");            // contact + footer
    }

    [Fact]
    public async Task HomePage_RendersEveryDisciplineAndOpeningDay()
    {
        var html = await GetHomePageAsync();

        foreach (var discipline in SiteContentDefaults.Disciplines)
        {
            html.Should().Contain(discipline.Title);
        }

        foreach (var hour in SiteContentDefaults.PracticalInfo.Hours)
        {
            html.Should().Contain(hour.Day);
        }
    }

    [Fact]
    public async Task Navigation_DoesNotLinkToMatches()
    {
        // The match feature moved out to a separate WordPress module; nothing here should
        // still point at the old /matchs route.
        var html = await GetHomePageAsync();

        html.Should().NotContain("/matchs");
    }

    [Fact]
    public async Task HomePage_ListsPublishedNewsNewestFirst()
    {
        await ReplaceNewsAsync(
            NewsItem("plus-ancienne", "Plus ancienne", publishedDaysAgo: 10),
            NewsItem("plus-recente", "Plus récente", publishedDaysAgo: 1),
            NewsItem("brouillon", "Brouillon caché", publishedDaysAgo: 2, published: false));

        var html = await GetHomePageAsync();

        html.Should().NotContain("Brouillon caché");
        html.IndexOf("Plus récente", StringComparison.Ordinal)
            .Should().BeLessThan(html.IndexOf("Plus ancienne", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HomePage_UsesStoredSectionContentOverTheDefaults()
    {
        const string customBadge = "BADGE PERSONNALISÉ POUR LE TEST";
        await StoreHeroOverrideAsync($$"""{"badge":"{{customBadge}}"}""");

        try
        {
            var html = await GetHomePageAsync();

            html.Should().Contain(customBadge);
            // Untouched fields still come from the defaults — that is the merge working.
            html.Should().Contain(SiteContentDefaults.Hero.Paragraph[..40]);
        }
        finally
        {
            await ClearHeroOverrideAsync();
        }
    }

    private async Task StoreHeroOverrideAsync(string json)
    {
        await using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AmtarcDbContext>();
        db.SiteContent.Add(new SiteContentEntry { Key = SectionKey.Hero, Data = json });
        await db.SaveChangesAsync();
    }

    private async Task ClearHeroOverrideAsync()
    {
        await using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AmtarcDbContext>();
        await db.SiteContent.Where(s => s.Key == SectionKey.Hero).ExecuteDeleteAsync();
    }

    private async Task ReplaceNewsAsync(params News[] items)
    {
        await using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AmtarcDbContext>();
        await db.News.ExecuteDeleteAsync();
        db.News.AddRange(items);
        await db.SaveChangesAsync();
    }

    private static News NewsItem(string slug, string title, int publishedDaysAgo, bool published = true) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Slug = slug,
        Title = title,
        Category = NewsCategory.Concours,
        Body = "Corps.",
        Published = published,
        PublishedAt = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-publishedDaysAgo), DateTimeKind.Unspecified),
    };
}
