using System.Net;

namespace Amtarc.Web.Tests.Integration;

/// <summary>
/// The small navigation behaviours around the back-office that are easy to leave untested
/// because nothing points at them until someone types the URL.
/// </summary>
[Collection(AmtarcWebCollection.Name)]
public sealed class AdminNavigationTests(AmtarcWebFactory factory)
{
    private readonly AmtarcWebFactory _factory = factory;

    [Fact]
    public async Task AdminRoot_SendsASignedInAdminToTheNewsScreen()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/admin");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        LoginClient.RedirectTarget(response).Should().Be("/admin/news");
    }

    [Fact]
    public async Task Logout_OverGet_JustShowsTheLoginPageAgain()
    {
        // Sign-out is POST-only so a third-party page cannot trigger it with an image tag; a GET
        // has to land somewhere sensible rather than 404.
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/admin/logout");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        LoginClient.RedirectTarget(response).Should().Be("/admin/login");
    }

    [Fact]
    public async Task Logout_OverGet_DoesNotEndTheSession()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        await client.GetAsync("/admin/logout");

        (await client.GetAsync("/admin/news")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
