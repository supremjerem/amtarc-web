namespace Amtarc.Web.Content;

/// <summary>A link rendered as a button or inline call to action.</summary>
public sealed record CallToAction(string Href, string Label);

/// <summary>An entry in the main navigation.</summary>
public sealed record NavLink(string Href, string Label);

public enum SocialPlatform
{
    Facebook,
    Instagram,
    TikTok,
    YouTube,
}

/// <summary>
/// A social account. <paramref name="ComingSoon"/> marks a platform the club has not opened yet —
/// it renders as a disabled pastille rather than a dead link.
/// </summary>
public sealed record SocialLink(
    SocialPlatform Platform,
    string Label,
    string Href,
    bool External,
    bool ComingSoon);
