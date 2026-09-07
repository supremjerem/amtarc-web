using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace Amtarc.Web.Tests.Integration;

/// <summary>
/// The back-office gate: who gets in, who is turned away, and what each of them is told.
/// </summary>
[Collection(AmtarcWebCollection.Name)]
public sealed class AdminAuthTests(AmtarcWebFactory factory)
{
    private readonly AmtarcWebFactory _factory = factory;

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/news")]
    public async Task AdminPage_RedirectsAnonymousVisitorToLogin(string path)
    {
        var response = await LoginClient.CreateClient(_factory).GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        LoginClient.RedirectTarget(response).Should().StartWith("/admin/login");
    }

    [Fact]
    public async Task AdminPage_RedirectCarriesTheRequestedPathAsReturnUrl()
    {
        var response = await LoginClient.CreateClient(_factory).GetAsync("/admin/news");

        LoginClient.RedirectTarget(response).Should().Contain("returnUrl=%2Fadmin%2Fnews");
    }

    [Fact]
    public async Task Login_WithValidCredentials_IssuesTheSessionCookieAndRedirectsToNews()
    {
        var client = LoginClient.CreateClient(_factory);

        var response = await LoginClient.SignInAsync(
            client, AmtarcWebFactory.AdminEmail, AmtarcWebFactory.AdminPassword);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        LoginClient.RedirectTarget(response).Should().Be("/admin/news");

        var cookie = response.Headers.GetValues("Set-Cookie")
            .Should().ContainSingle(c => c.StartsWith("amtarc_admin=", StringComparison.Ordinal))
            .Subject;

        cookie.Should().Contain("httponly", "the session cookie must not be readable from script");
        cookie.Should().Contain("samesite=lax");
        // A session cookie has neither expires nor max-age: closing the browser signs the admin out.
        cookie.Should().NotContain("expires=").And.NotContain("max-age=");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ThenReachesTheGatedPage()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/admin/news");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Se déconnecter");
    }

    [Theory]
    [InlineData(AmtarcWebFactory.AdminEmail, "not-the-password")]
    [InlineData("nobody@amtarc.test", AmtarcWebFactory.AdminPassword)]
    public async Task Login_WithBadCredentials_RedisplaysTheFormWithOneGenericError(
        string email, string password)
    {
        var client = LoginClient.CreateClient(_factory);

        var response = await LoginClient.SignInAsync(client, email, password);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        (cookies ?? [])
            .Should().NotContain(c => c.StartsWith("amtarc_admin=", StringComparison.Ordinal));

        // Identical wording either way — a different message for an unknown address would let
        // anyone probe which addresses are admins.
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Identifiants invalides.");
    }

    [Fact]
    public async Task Login_WhenAlreadySignedIn_RedirectsStraightToNews()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/admin/login");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        LoginClient.RedirectTarget(response).Should().Be("/admin/news");
    }

    [Fact]
    public async Task Login_HonoursALocalReturnUrl()
    {
        var client = LoginClient.CreateClient(_factory);

        var response = await LoginClient.SignInAsync(
            client, AmtarcWebFactory.AdminEmail, AmtarcWebFactory.AdminPassword, "/admin/news");

        LoginClient.RedirectTarget(response).Should().Be("/admin/news");
    }

    [Fact]
    public async Task Login_IgnoresAnOffSiteReturnUrl()
    {
        var client = LoginClient.CreateClient(_factory);

        var response = await LoginClient.SignInAsync(
            client,
            AmtarcWebFactory.AdminEmail,
            AmtarcWebFactory.AdminPassword,
            "https://evil.example/phish");

        LoginClient.RedirectTarget(response).Should().Be("/admin/news");
    }

    [Fact]
    public async Task Login_WithoutAnAntiForgeryToken_IsRejected()
    {
        var client = LoginClient.CreateClient(_factory);

        var response = await client.PostAsync("/admin/login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Input.Email"] = AmtarcWebFactory.AdminEmail,
                ["Input.Password"] = AmtarcWebFactory.AdminPassword,
            }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_ClearsTheCookieAndTheGatedPageBecomesUnreachableAgain()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var token = LoginClient.ExtractAntiForgeryToken(await client.GetStringAsync("/admin/news"));

        var response = await client.PostAsync("/admin/logout", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        LoginClient.RedirectTarget(response).Should().Be("/admin/login");

        var afterLogout = await client.GetAsync("/admin/news");
        afterLogout.StatusCode.Should().Be(HttpStatusCode.Found);
        LoginClient.RedirectTarget(afterLogout).Should().StartWith("/admin/login");
    }

    [Fact]
    public async Task AdminPage_IsNotIndexable()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var html = await client.GetStringAsync("/admin/news");

        html.Should().Contain("noindex");
    }

    /// <summary>
    /// The lockout runs on its own host with a tiny window: hammering the shared one would lock
    /// every other test in this collection out for the whole run.
    /// </summary>
    [Fact]
    public async Task Login_AfterTooManyAttempts_SaysSoDistinctlyFromABadPassword()
    {
        const int limit = 3;

        using var throttled = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Security:LoginAttemptLimit", limit.ToString(CultureInfo.InvariantCulture));
            builder.UseSetting("Security:LoginWindowSeconds", "60");
        });

        var client = LoginClient.CreateClient(throttled);

        for (var attempt = 0; attempt < limit; attempt++)
        {
            var allowed = await LoginClient.SignInAsync(
                client, AmtarcWebFactory.AdminEmail, "not-the-password");
            allowed.StatusCode.Should().Be(HttpStatusCode.OK, "attempt {0} is within the limit", attempt + 1);
        }

        var blocked = await LoginClient.SignInAsync(
            client, AmtarcWebFactory.AdminEmail, "not-the-password");

        blocked.StatusCode.Should().Be(HttpStatusCode.Found);
        LoginClient.RedirectTarget(blocked).Should().Be("/admin/login?lockout=true");
        blocked.Headers.RetryAfter.Should().NotBeNull();

        // The point of the flag: the page tells the visitor they are throttled, not that they
        // mistyped their password.
        var page = await client.GetStringAsync("/admin/login?lockout=true");
        page.Should().Contain("Trop de tentatives de connexion.");
        page.Should().NotContain("Identifiants invalides.");
    }
}
