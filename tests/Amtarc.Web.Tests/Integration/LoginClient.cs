using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Amtarc.Web.Tests.Integration;

/// <summary>
/// Signs in through the real login page rather than forging a cookie, so the anti-forgery token
/// and the form binding are exercised by every test that needs an authenticated client.
/// </summary>
public static partial class LoginClient
{
    /// <summary>
    /// A client that keeps cookies and reports redirects instead of following them. It talks https
    /// because outside Development the admin cookie is marked <c>Secure</c> — as in production
    /// behind Traefik — and a client on plain http silently drops it, which would look here like
    /// an authorization failure rather than a transport one.
    /// </summary>
    public static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

    public static async Task<HttpResponseMessage> SignInAsync(
        HttpClient client, string email, string password, string? returnUrl = null)
    {
        var token = await FetchAntiForgeryTokenAsync(client);
        var path = returnUrl is null
            ? "/admin/login"
            : $"/admin/login?returnUrl={Uri.EscapeDataString(returnUrl)}";

        return await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = token,
        }));
    }

    /// <summary>A client already carrying a valid admin session cookie.</summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        WebApplicationFactory<Program> factory)
    {
        var client = CreateClient(factory);
        var response = await SignInAsync(client, AmtarcWebFactory.AdminEmail, AmtarcWebFactory.AdminPassword);

        response.StatusCode.Should().Be(HttpStatusCode.Found, "the seeded credentials must sign in");

        // Assert on the cookie, not just the redirect: a throttled attempt also answers 302, and
        // without this check the helper would hand back an anonymous client that fails much later
        // as a puzzling authorization error.
        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        (cookies ?? []).Should().Contain(
            c => c.StartsWith("amtarc_admin=", StringComparison.Ordinal),
            "signing in must issue the session cookie");

        return client;
    }

    /// <summary>
    /// The redirect target as path + query. The cookie handler's challenge writes an absolute URI
    /// while <c>Redirect("/admin/news")</c> writes a relative one; tests care about neither.
    /// </summary>
    public static string RedirectTarget(HttpResponseMessage response)
    {
        var location = response.Headers.Location;
        location.Should().NotBeNull("the response must be a redirect");

        return location!.IsAbsoluteUri
            ? location.PathAndQuery
            : location.OriginalString;
    }

    /// <summary>Pulls the hidden anti-forgery field out of any rendered admin form.</summary>
    public static string ExtractAntiForgeryToken(string html)
    {
        var match = AntiForgeryField().Match(html);

        match.Success.Should().BeTrue("every admin form must carry an anti-forgery token");
        return match.Groups["token"].Value;
    }

    private static async Task<string> FetchAntiForgeryTokenAsync(HttpClient client) =>
        ExtractAntiForgeryToken(await client.GetStringAsync("/admin/login"));

    [GeneratedRegex(
        """<input name="__RequestVerificationToken" type="hidden" value="(?<token>[^"]+)""")]
    private static partial Regex AntiForgeryField();
}
