namespace Amtarc.Web.Infrastructure;

/// <summary>
/// The subset of Helmet the previous site actually relied on, plus a content-security policy.
/// There is no CORS configuration any more: with the API folded into the app there are no
/// cross-origin callers to allow.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>
    /// No <c>'unsafe-inline'</c> anywhere: the page's script is a single bundled file and the
    /// ported markup carries no inline <c>style</c> attributes — the hero's animation delays and
    /// the honeycomb opacity are Tailwind classes precisely so this can stay strict.
    /// <c>img-src</c> allows https because a news item may point at an image hosted elsewhere,
    /// and data: because the fallback favicon is inlined.
    /// </summary>
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'; " +
        "object-src 'none'";

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["Content-Security-Policy"] = ContentSecurityPolicy;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        // Redundant with frame-ancestors for modern browsers, kept for older ones.
        headers["X-Frame-Options"] = "DENY";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";

        await next(context);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
