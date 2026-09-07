using System.Net;
using Amtarc.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amtarc.Web.Tests.Integration;

/// <summary>
/// The news back-office end to end: what an admin writes lands in the database and shows up on
/// the public page, and what they delete disappears from both.
/// </summary>
[Collection(AmtarcWebCollection.Name)]
public sealed class AdminNewsCrudTests(AmtarcWebFactory factory)
{
    private readonly AmtarcWebFactory _factory = factory;

    private static Dictionary<string, string> ValidItem(string title) => new()
    {
        ["Input.Title"] = title,
        ["Input.Category"] = "Travaux",
        ["Input.Excerpt"] = "Résumé de l'actualité.",
        ["Input.Body"] = "Le texte complet de l'actualité.",
        ["Input.Published"] = "true",
    };

    [Theory]
    [InlineData("/admin/news")]
    [InlineData("/admin/news/create")]
    [InlineData("/admin/content")]
    [InlineData("/admin/content/hero")]
    public async Task AdminScreen_IsClosedToAnonymousVisitors(string path)
    {
        var response = await LoginClient.CreateClient(_factory).GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        LoginClient.RedirectTarget(response).Should().StartWith("/admin/login");
    }

    [Fact]
    public async Task Create_StoresTheItem_AndItAppearsOnThePublicPage()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var title = $"Nouvelle installation {Guid.NewGuid():N}";

        var response = await AdminForm.PostAsync(client, "/admin/news/create", ValidItem(title));

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        LoginClient.RedirectTarget(response).Should().Be("/admin/news");

        var listing = await client.GetStringAsync("/admin/news");
        listing.Should().Contain(title);

        var publicPage = await _factory.CreateClient().GetStringAsync("/");
        publicPage.Should().Contain(title);
    }

    [Fact]
    public async Task Create_DerivesTheSlugFromTheTitle()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var marker = Guid.NewGuid().ToString("N")[..8];

        await AdminForm.PostAsync(client, "/admin/news/create", ValidItem($"Réfection du pas de tir {marker}"));

        var item = await FindByTitleAsync($"Réfection du pas de tir {marker}");
        item.Slug.Should().Be($"refection-du-pas-de-tir-{marker}");
    }

    [Fact]
    public async Task Create_AppendsASuffix_WhenTheSlugIsAlreadyTaken()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var title = $"Bourse aux armes {Guid.NewGuid().ToString("N")[..8]}";

        await AdminForm.PostAsync(client, "/admin/news/create", ValidItem(title));
        await AdminForm.PostAsync(client, "/admin/news/create", ValidItem(title));

        var slugs = await SlugsForTitleAsync(title);
        slugs.Should().HaveCount(2);
        slugs.Should().ContainSingle(slug => slug.EndsWith("-2", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Create_RejectsATooShortTitle_AndRedisplaysTheForm()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var fields = ValidItem("ab");

        var response = await AdminForm.PostAsync(client, "/admin/news/create", fields);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("au moins 3 caractères");
    }

    [Fact]
    public async Task Create_AsADraft_KeepsItOffThePublicPage()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var title = $"Brouillon {Guid.NewGuid():N}";
        var fields = ValidItem(title);
        fields["Input.Published"] = "false";

        await AdminForm.PostAsync(client, "/admin/news/create", fields);

        var listing = await client.GetStringAsync("/admin/news");
        listing.Should().Contain(title).And.Contain("Brouillon");

        var publicPage = await _factory.CreateClient().GetStringAsync("/");
        publicPage.Should().NotContain(title);
    }

    [Fact]
    public async Task Edit_UpdatesTheItem_ButKeepsTheSlugItWasPublishedUnder()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var original = $"Titre initial {Guid.NewGuid().ToString("N")[..8]}";
        await AdminForm.PostAsync(client, "/admin/news/create", ValidItem(original));

        var item = await FindByTitleAsync(original);
        var renamed = $"Titre corrigé {Guid.NewGuid().ToString("N")[..8]}";
        var fields = ValidItem(renamed);

        var response = await AdminForm.PostAsync(client, $"/admin/news/{item.Id}", fields);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        var updated = await FindByIdAsync(item.Id);
        updated.Title.Should().Be(renamed);
        // The slug is the public handle — renaming the title must not break links already shared.
        updated.Slug.Should().Be(item.Slug);
    }

    [Fact]
    public async Task Edit_UnpublishingRemovesTheItemFromThePublicPage()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var title = $"À dépublier {Guid.NewGuid():N}";
        await AdminForm.PostAsync(client, "/admin/news/create", ValidItem(title));
        var item = await FindByTitleAsync(title);

        var fields = ValidItem(title);
        fields["Input.Published"] = "false";
        await AdminForm.PostAsync(client, $"/admin/news/{item.Id}", fields);

        var publicPage = await _factory.CreateClient().GetStringAsync("/");
        publicPage.Should().NotContain(title);
    }

    [Fact]
    public async Task Edit_ReturnsNotFound_ForAnIdThatDoesNotExist()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync($"/admin/news/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_RemovesTheItemFromTheListingAndThePublicPage()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var title = $"À supprimer {Guid.NewGuid():N}";
        await AdminForm.PostAsync(client, "/admin/news/create", ValidItem(title));
        var item = await FindByTitleAsync(title);

        var response = await AdminForm.PostAsync(
            client, "/admin/news?handler=Delete", new Dictionary<string, string> { ["id"] = item.Id },
            formPath: "/admin/news");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        (await client.GetStringAsync("/admin/news")).Should().NotContain(title);
        (await _factory.CreateClient().GetStringAsync("/")).Should().NotContain(title);
    }

    [Fact]
    public async Task Write_WithoutAnAntiForgeryToken_IsRejected()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var response = await client.PostAsync(
            "/admin/news/create", new FormUrlEncodedContent(ValidItem("Sans jeton anti-CSRF")));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<Web.Domain.News> FindByTitleAsync(string title)
    {
        await using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AmtarcDbContext>();

        return await db.News.AsNoTracking().SingleAsync(n => n.Title == title);
    }

    private async Task<Web.Domain.News> FindByIdAsync(string id)
    {
        await using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AmtarcDbContext>();

        return await db.News.AsNoTracking().SingleAsync(n => n.Id == id);
    }

    private async Task<List<string>> SlugsForTitleAsync(string title)
    {
        await using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AmtarcDbContext>();

        return await db.News.AsNoTracking()
            .Where(n => n.Title == title)
            .Select(n => n.Slug)
            .ToListAsync();
    }
}
