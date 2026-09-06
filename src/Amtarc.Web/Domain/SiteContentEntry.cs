namespace Amtarc.Web.Domain;

/// <summary>
/// One editable page section, stored as JSON and deep-merged over the built-in defaults
/// at render time. A row exists only for a section an admin has actually edited.
/// </summary>
public class SiteContentEntry : ITimestamped
{
    /// <summary>The section name — e.g. <c>hero</c>, <c>announcements</c>, <c>practical-info</c>, <c>contact</c>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Raw JSON payload for the section (stored in a <c>jsonb</c> column).</summary>
    public string Data { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; }
}
