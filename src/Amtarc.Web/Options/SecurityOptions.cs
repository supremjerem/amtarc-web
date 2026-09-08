using System.ComponentModel.DataAnnotations;

namespace Amtarc.Web.Options;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Reverse-proxy hops in front of the app: 0 when it is exposed directly, 1 behind Traefik.
    /// Rate limiting and logs need the forwarded client IP, and trusting one hop too many lets a
    /// caller spoof it. Port of the previous site's <c>TRUST_PROXY</c>.
    /// </summary>
    [Range(0, 8, ErrorMessage = "Security:TrustProxyHops must be between 0 and 8.")]
    public int TrustProxyHops { get; set; }

    [Range(1, 100_000, ErrorMessage = "Security:RequestLimit must be at least 1.")]
    public int RequestLimit { get; set; } = 120;

    [Range(1, 3600, ErrorMessage = "Security:RequestWindowSeconds must be between 1 and 3600.")]
    public int RequestWindowSeconds { get; set; } = 60;

    [Range(1, 100_000, ErrorMessage = "Security:LoginAttemptLimit must be at least 1.")]
    public int LoginAttemptLimit { get; set; } = 10;

    [Range(1, 3600, ErrorMessage = "Security:LoginWindowSeconds must be between 1 and 3600.")]
    public int LoginWindowSeconds { get; set; } = 300;

    /// <summary>How long the public page may be served from the output cache.</summary>
    [Range(0, 86_400, ErrorMessage = "Security:PublicCacheSeconds must be between 0 and 86400.")]
    public int PublicCacheSeconds { get; set; } = 300;
}
