using System.Globalization;
using System.Threading.RateLimiting;
using Amtarc.Web.Options;
using Amtarc.Web.Security;

namespace Amtarc.Web.Infrastructure;

/// <summary>
/// Two throttles, both partitioned by client IP: a broad one over every endpoint, and a much
/// tighter one on sign-in. Port of the previous site's Nest throttler.
/// </summary>
public static class RateLimiterSetup
{
    public static IServiceCollection AddAmtarcRateLimiter(
        this IServiceCollection services, SecurityOptions security)
    {
        var loginWindow = TimeSpan.FromSeconds(security.LoginWindowSeconds);

        return services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = security.RequestLimit,
                        Window = TimeSpan.FromSeconds(security.RequestWindowSeconds),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(AuthenticationSetup.LoginRateLimitPolicy, context =>
            {
                // Razor Pages attaches rate-limiting metadata per page, not per handler, so this
                // policy sees the GET of the form as well as the POST. Only sign-in attempts may
                // consume the budget — otherwise reloading the page locks you out of it.
                if (!HttpMethods.IsPost(context.Request.Method))
                {
                    return RateLimitPartition.GetNoLimiter("non-post");
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = security.LoginAttemptLimit,
                        Window = loginWindow,
                        QueueLimit = 0,
                    });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = (context, _) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value)
                    ? value
                    : loginWindow;

                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);

                // A blocked sign-in goes back to the form with a flag rather than to a bare 429,
                // so the page can say "too many attempts" — a lockout must not read as "wrong
                // password". Everything else just gets the status code.
                if (IsLoginPost(context.HttpContext.Request))
                {
                    context.HttpContext.Response.Redirect($"{AuthenticationSetup.LoginPath}?lockout=true");
                }

                return ValueTask.CompletedTask;
            };
        });
    }

    private static bool IsLoginPost(HttpRequest request) =>
        HttpMethods.IsPost(request.Method)
        && request.Path.StartsWithSegments(AuthenticationSetup.LoginPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The forwarded-headers middleware has already resolved the real address by the time this
    /// runs, so partitions are per visitor rather than per proxy.
    /// </summary>
    private static string ClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
