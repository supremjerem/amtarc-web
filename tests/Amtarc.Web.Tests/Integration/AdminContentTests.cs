using System.Net;
using Amtarc.Web.Content;
using Amtarc.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Amtarc.Web.Tests.Integration;

/// <summary>
/// Editing the four sections whose copy an admin owns: what they type reaches the public page,
/// the fields they did not touch survive, and resetting hands the section back to the defaults.
/// </summary>
[Collection(AmtarcWebCollection.Name)]
public sealed class AdminContentTests(AmtarcWebFactory factory) : IAsyncLifetime
{
    private readonly AmtarcWebFactory _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Every section goes back to its defaults afterwards — these tests share one database with
    /// the public-page tests, which assert on the default copy.
    /// </summary>
    public async Task DisposeAsync()
    {
        await using var scope = _factory.CreateScope();
        var content = scope.ServiceProvider.GetRequiredService<ISiteContentService>();

        foreach (var key in SectionKey.All)
        {
            await content.ResetAsync(key);
        }
    }

    [Fact]
    public async Task ContentIndex_ListsTheFourEditableSections()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        // Decoded, because Razor escapes the apostrophe in titles like "Bandeau d'accueil".
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/admin/content"));

        foreach (var section in SectionFieldConfig.All)
        {
            html.Should().Contain(section.Title);
        }
    }

    [Fact]
    public async Task EditScreen_IsPrefilledWithWhatTheSectionCurrentlyRenders()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var html = await client.GetStringAsync("/admin/content/hero");

        html.Should().Contain("Fields[badge]").And.Contain("Fields[ctaPrimary.label]");
    }

    [Fact]
    public async Task EditScreen_ReturnsNotFound_ForASectionThatIsNotEditable()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/admin/content/club-stats");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Save_PutsTheNewCopyOnThePublicPage()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var badge = $"Saison {Guid.NewGuid().ToString("N")[..6]}";

        var response = await AdminForm.PostAsync(client, "/admin/content/hero", new Dictionary<string, string>
        {
            ["Fields[badge]"] = badge,
            ["Fields[title]"] = "Tirer juste, ensemble",
            ["Fields[paragraph]"] = "Un stand, un club, une saison.",
            ["Fields[ctaPrimary.label]"] = "Nous rejoindre",
            ["Fields[ctaSecondary.label]"] = "Nous écrire",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        var publicPage = await _factory.CreateClient().GetStringAsync("/");
        publicPage.Should().Contain(badge).And.Contain("Tirer juste, ensemble");
    }

    [Fact]
    public async Task Save_KeepsFieldsTheFormDoesNotExpose()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        // The form offers the CTA labels but not their hrefs; saving must not blank the hrefs.
        await AdminForm.PostAsync(client, "/admin/content/hero", new Dictionary<string, string>
        {
            ["Fields[badge]"] = "Badge",
            ["Fields[title]"] = "Titre",
            ["Fields[paragraph]"] = "Paragraphe",
            ["Fields[ctaPrimary.label]"] = "Nous rejoindre",
            ["Fields[ctaSecondary.label]"] = "Nous écrire",
        });

        await using var scope = _factory.CreateScope();
        var stored = await scope.ServiceProvider
            .GetRequiredService<ISiteContentService>()
            .GetSectionAsync(SectionKey.Hero);

        stored["ctaPrimary"]!["href"].Should().NotBeNull();
    }

    [Fact]
    public async Task Save_StoresALinesFieldAsAnArray_DroppingBlankLines()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        await AdminForm.PostAsync(client, "/admin/content/contact", new Dictionary<string, string>
        {
            ["Fields[heading]"] = "Nous trouver",
            ["Fields[paragraph]"] = "Le stand est à Chapas.",
            ["Fields[email]"] = "contact@amtarc.test",
            ["Fields[address.lines]"] = "Stand de Chapas\n\n82290 Meauzac\n",
            ["Fields[hours.lines]"] = "Samedi 14h-18h",
        });

        await using var scope = _factory.CreateScope();
        var stored = await scope.ServiceProvider
            .GetRequiredService<ISiteContentService>()
            .GetSectionAsync(SectionKey.Contact);

        stored["address"]!["lines"]!.AsArray()
            .Select(line => line!.GetValue<string>())
            .Should().Equal("Stand de Chapas", "82290 Meauzac");
    }

    [Fact]
    public async Task Save_StoresScheduleRowsAsDayTimePairs_DroppingEmptyOnes()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        await AdminForm.PostAsync(client, "/admin/content/practical-info", new Dictionary<string, string>
        {
            ["Fields[hoursKicker]"] = "Horaires",
            ["Fields[hoursNote]"] = "Fermé les jours fériés.",
            ["Fields[membershipKicker]"] = "Adhésion",
            ["Fields[membershipHeading]"] = "Rejoindre le club",
            ["Fields[membershipChecklist]"] = "Pièce d'identité\nPhoto",
            ["Fields[membershipNote]"] = "Dossier à déposer au stand.",
            ["Fields[membershipCta.label]"] = "Télécharger",
            ["HourRows[0].Path"] = "hours",
            ["HourRows[0].Day"] = "Mercredi",
            ["HourRows[0].Time"] = "14h — 18h",
            ["HourRows[1].Path"] = "hours",
            ["HourRows[1].Day"] = "",
            ["HourRows[1].Time"] = "",
        });

        await using var scope = _factory.CreateScope();
        var stored = await scope.ServiceProvider
            .GetRequiredService<ISiteContentService>()
            .GetSectionAsync(SectionKey.PracticalInfo);

        var hours = stored["hours"]!.AsArray();
        hours.Should().ContainSingle();
        hours[0]!["day"]!.GetValue<string>().Should().Be("Mercredi");
        hours[0]!["time"]!.GetValue<string>().Should().Be("14h — 18h");
    }

    [Fact]
    public async Task AddRow_RedisplaysTheFormWithAnExtraRow_WithoutSaving()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var response = await AdminForm.PostAsync(
            client,
            "/admin/content/practical-info?handler=AddRow&path=hours",
            new Dictionary<string, string>
            {
                ["Fields[hoursKicker]"] = "Horaires en cours de saisie",
                ["HourRows[0].Path"] = "hours",
                ["HourRows[0].Day"] = "Mercredi",
                ["HourRows[0].Time"] = "14h — 18h",
            },
            formPath: "/admin/content/practical-info");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("HourRows[1].Day", "the extra row must be rendered");
        // Text typed but not yet saved has to survive the round-trip, or adding a row silently
        // discards the edit in progress.
        html.Should().Contain("Horaires en cours de saisie");

        await using var scope = _factory.CreateScope();
        var stored = await scope.ServiceProvider
            .GetRequiredService<ISiteContentService>()
            .GetSectionAsync(SectionKey.PracticalInfo);
        stored["hoursKicker"]!.GetValue<string>().Should().NotBe("Horaires en cours de saisie");
    }

    [Fact]
    public async Task RemoveRow_RedisplaysTheFormWithoutThatRow()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var response = await AdminForm.PostAsync(
            client,
            "/admin/content/practical-info?handler=RemoveRow&index=0",
            new Dictionary<string, string>
            {
                ["HourRows[0].Path"] = "hours",
                ["HourRows[0].Day"] = "À supprimer",
                ["HourRows[0].Time"] = "00h — 00h",
                ["HourRows[1].Path"] = "hours",
                ["HourRows[1].Day"] = "À garder",
                ["HourRows[1].Time"] = "14h — 18h",
            },
            formPath: "/admin/content/practical-info");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("À garder").And.NotContain("À supprimer");
    }

    [Fact]
    public async Task Reset_HandsTheSectionBackToTheBuiltInDefaults()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var badge = $"Temporaire {Guid.NewGuid().ToString("N")[..6]}";

        await AdminForm.PostAsync(client, "/admin/content/hero", new Dictionary<string, string>
        {
            ["Fields[badge]"] = badge,
            ["Fields[title]"] = "Titre temporaire",
            ["Fields[paragraph]"] = "Paragraphe temporaire",
            ["Fields[ctaPrimary.label]"] = "Bouton",
            ["Fields[ctaSecondary.label]"] = "Autre bouton",
        });
        (await _factory.CreateClient().GetStringAsync("/")).Should().Contain(badge);

        var response = await AdminForm.PostAsync(
            client, "/admin/content/hero?handler=Reset", new Dictionary<string, string>(), formPath: "/admin/content/hero");

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        var publicPage = await _factory.CreateClient().GetStringAsync("/");
        publicPage.Should().NotContain(badge);
        publicPage.Should().Contain(SiteContentDefaults.Hero.Badge);
    }
}
