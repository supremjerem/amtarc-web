using Microsoft.AspNetCore.Authentication.Cookies;

namespace Amtarc.Web.Security;

/// <summary>
/// Cookie authentication for the single back-office account. The sign-in throttle named here
/// by <see cref="LoginRateLimitPolicy"/> is configured in
/// <see cref="Infrastructure.RateLimiterSetup"/>, alongside the global one.
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
}
