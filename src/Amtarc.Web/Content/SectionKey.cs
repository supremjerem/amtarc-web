namespace Amtarc.Web.Content;

/// <summary>
/// The four page sections an admin can edit. These strings are the primary keys of the
/// <c>SiteContent</c> table, so they are part of the stored data contract — they match the keys
/// the previous site wrote.
/// </summary>
public static class SectionKey
{
    public const string Hero = "hero";
    public const string Announcements = "announcements";
    public const string PracticalInfo = "practical-info";
    public const string Contact = "contact";

    public static readonly IReadOnlyList<string> All =
        [Hero, Announcements, PracticalInfo, Contact];
}
