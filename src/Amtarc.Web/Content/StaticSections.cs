namespace Amtarc.Web.Content;

/*
 * Sections that are not editable from the back-office. They change with the site's structure
 * rather than with club news, so they live in code.
 */

public enum StatVariant
{
    Light,
    Gold,
    Dark,
}

/// <summary>A counted figure in the club stats grid — <see cref="Value"/> animates up on scroll.</summary>
public sealed record ClubStat(int Value, string Label, StatVariant Variant, string? Suffix = null);

public sealed record ClubIntro
{
    public required string Kicker { get; init; }
    public required string Heading { get; init; }
    public required string Paragraph { get; init; }
    public required IReadOnlyList<string> Features { get; init; }
}

/// <summary>How much room a discipline tile takes in the bento grid.</summary>
public enum DisciplineSize
{
    Hero,
    Small,
    Wide,
    Full,
}

public sealed record DisciplineCard
{
    public required string Id { get; init; }
    public required DisciplineSize Size { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public string? Overline { get; init; }
    public string? Badge { get; init; }
    public string? Href { get; init; }
    public string? Cta { get; init; }

    /// <summary>Oversized watermark figure on the wide tile (e.g. "100m").</summary>
    public string? BigLabel { get; init; }

    public string? Emoji { get; init; }
}

public sealed record SectionHeading(string Kicker, string Heading);

public sealed record TsvStat(string Value, string Label);

public sealed record TsvShowcaseContent
{
    public required string Badge { get; init; }
    public required string Heading { get; init; }
    public required IReadOnlyList<string> Paragraphs { get; init; }
    public required IReadOnlyList<TsvStat> Stats { get; init; }
    public required CallToAction CtaPrimary { get; init; }
    public required CallToAction CtaSecondary { get; init; }
}

public sealed record FooterLink(string Href, string Label, bool External = false);

public sealed record FooterColumn(string Title, IReadOnlyList<FooterLink> Links);

public sealed record FooterContent
{
    public required string Tagline { get; init; }
    public required IReadOnlyList<FooterColumn> Columns { get; init; }
    public required string Location { get; init; }
}
