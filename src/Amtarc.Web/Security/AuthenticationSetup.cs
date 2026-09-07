using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Amtarc.Web.Security;

/// <summary>
/// Cookie authentication for the single back-office account, and the sign-in throttle.
/// See docs/adr/0002-cookie-auth-for-the-single-admin.md.
/// </summary>
public static class AuthenticationSetup
{
    public const string Scheme = "admin";
    public const string AdminOnlyPolicy = "AdminOnly";
    public const string LoginRateLimitPolicy = "login";
    public const string RoleClaim = "role";
    public const string RoleClaimValue = "admin";

    public const string LoginPath = "/admin/login";
    public const string LandingPath = "/admin/news";

    /// <summary>Sign-in attempts allowed per window, matching the previous NestJS throttle.</summary>
    private const int DefaultAttemptLimit = 10;
    private const int DefaultWindowSeconds = 300;

    public static IServiceCollection AddAdminAuthentication(
        this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddAuthentication(Scheme)
            .AddCookie(Scheme, options =>
            {
                options.Cookie.Name = "amtarc_admin";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;

                // Development runs on plain http; anywhere else the cookie must never leave over it.
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;

                // Sign-in issues a non-persistent cookie, so closing the browser ends the session;
                // the sliding window caps how long an idle session stays valid.
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;

                options.LoginPath = LoginPath;
                options.LogoutPath = "/admin/logout";
                options.AccessDeniedPath = LoginPath;
                options.ReturnUrlParameter = "returnUrl";
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AdminOnlyPolicy, policy => policy
                .AddAuthenticationSchemes(Scheme)
                .RequireAuthenticatedUser()
                .RequireClaim(RoleClaim, RoleClaimValue));

        return services;
    }

    /// <summary>
    /// Throttles sign-in attempts per client IP. The global limiter arrives with the rest of the
    /// hardening; this one exists already because an unthrottled login is exactly what a password
    /// guesser needs. The limits are configurable so tests can trip the lockout in a few requests.
    /// </summary>
    public static IServiceCollection AddLoginRateLimiter(
        this IServiceCollection services, IConfiguration configuration)
    {
        var limit = configuration.GetValue("Security:LoginAttemptLimit", DefaultAttemptLimit);
        var window = TimeSpan.FromSeconds(
            configuration.GetValue("Security:LoginWindowSeconds", DefaultWindowSeconds));

        return services.AddRateLimiter(options =>
        {
            options.AddPolicy(LoginRateLimitPolicy, context =>
            {
                // Razor Pages attaches rate-limiting metadata per page, not per handler, so this
                // policy sees the GET of the form as well as the POST. Only sign-in attempts may
                // consume the budget — otherwise reloading the page locks you out of it.
                if (!HttpMethods.IsPost(context.Request.Method))
                {
                    return RateLimitPartition.GetNoLimiter("non-post");
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit,
                        Window = window,
                        QueueLimit = 0,
                    });
            });

            options.OnRejected = (context, _) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value)
                    ? value
                    : window;

                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);

                // Send the visitor back to the form with a flag rather than to a bare 429, so the
                // page can say "too many attempts" — a lockout must not read as "wrong password".
                context.HttpContext.Response.Redirect($"{LoginPath}?lockout=true");
                return ValueTask.CompletedTask;
            };
        });
    }
}
