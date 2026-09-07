namespace Amtarc.Web.Content;

/*
 * The four sections an admin can edit. Each is stored as JSON in the SiteContent table and
 * deep-merged over the defaults in SiteContentDefaults at render time, so the property names
 * here are part of the stored contract — they serialise to the camelCase keys the previous
 * site wrote (hero.ctaPrimary.label, announcements.medicalCertificate.badge, ...).
 *
 * Sections that are not editable (club stats, disciplines, the TSV showcase, the news heading,
 * the footer) live in SiteContentDefaults as plain constants instead.
 */

public sealed record HeroContent
{
    public required string Badge { get; init; }
    public required string Wordmark { get; init; }

    /// <summary>Rendered with line breaks preserved — the copy carries its own wrap.</summary>
    public required string Title { get; init; }

    public required string Paragraph { get; init; }
    public required CallToAction CtaPrimary { get; init; }
    public required CallToAction CtaSecondary { get; init; }
    public required string ScrollHint { get; init; }
}

public sealed record AnnouncementCard
{
    public string? Kicker { get; init; }
    public string? Badge { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required CallToAction Cta { get; init; }
}

public sealed record AnnouncementsContent
{
    public required string Kicker { get; init; }
    public required string Heading { get; init; }
    public required CallToAction MoreLink { get; init; }
    public required AnnouncementCard MedicalCertificate { get; init; }
    public required AnnouncementCard Membership { get; init; }
    public required AnnouncementCard Merch { get; init; }
    public required AnnouncementCard Newsletter { get; init; }
}

public sealed record OpeningHour(string Day, string Time);

public sealed record PracticalInfoContent
{
    public required string HoursKicker { get; init; }
    public required IReadOnlyList<OpeningHour> Hours { get; init; }
    public required string HoursNote { get; init; }
    public required string MembershipKicker { get; init; }
    public required string MembershipHeading { get; init; }
    public required IReadOnlyList<string> MembershipChecklist { get; init; }
    public required string MembershipNote { get; init; }
    public required CallToAction MembershipCta { get; init; }
}

/// <summary>A labelled block of lines — the address and opening times under the contact card.</summary>
public sealed record ContactBlock
{
    public required string Kicker { get; init; }
    public required IReadOnlyList<string> Lines { get; init; }
}

public sealed record ContactContent
{
    public required string Heading { get; init; }
    public required string Paragraph { get; init; }
    public required string Email { get; init; }
    public required ContactBlock Address { get; init; }
    public required ContactBlock Hours { get; init; }
}
