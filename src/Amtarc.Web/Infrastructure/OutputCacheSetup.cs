namespace Amtarc.Web.Infrastructure;

/// <summary>
/// Caching for the one public page. This replaces the previous architecture's whole revalidation
/// path — the webhook route, its shared secret, and the service that called it — with an in-process
/// cache the admin handlers evict directly.
/// </summary>
public static class OutputCacheSetup
{
    /// <summary>Evicted when a news item is written.</summary>
    public const string NewsTag = "news";

    /// <summary>Evicted when an editable section is saved or reset.</summary>
    public const string ContentTag = "content";

    /// <summary>The policy the public page opts into.</summary>
    public const string PublicPagePolicy = "public-page";

    public static IServiceCollection AddPublicPageOutputCache(
        this IServiceCollection services, TimeSpan expiry) =>
        services.AddOutputCache(options =>
            options.AddPolicy(PublicPagePolicy, policy =>
            {
                // A zero expiry turns caching off outright — useful while working on the page
                // locally, and what the test suite runs with so a cached copy cannot mask a write.
                if (expiry <= TimeSpan.Zero)
                {
                    policy.NoCache();
                    return;
                }

                policy
                    .Expire(expiry)
                    .Tag(NewsTag, ContentTag)
                    // Anonymous visitors only: a signed-in admin must see their own change
                    // straight away, and nothing on the page varies by anything else.
                    .With(context => context.HttpContext.User.Identity?.IsAuthenticated != true);
            }));
}
