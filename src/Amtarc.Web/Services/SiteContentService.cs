using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Amtarc.Web.Content;
using Amtarc.Web.Data;
using Amtarc.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace Amtarc.Web.Services;

/// <summary>The four editable sections, each already merged over its built-in defaults.</summary>
public sealed record SiteContent
{
    public required HeroContent Hero { get; init; }
    public required AnnouncementsContent Announcements { get; init; }
    public required PracticalInfoContent PracticalInfo { get; init; }
    public required ContactContent Contact { get; init; }
}

public interface ISiteContentService
{
    Task<SiteContent> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// One section as JSON, already merged over its defaults — what the admin form binds to and
    /// what the public page will render.
    /// </summary>
    Task<JsonObject> GetSectionAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a section's override. The payload is merged over the defaults first, so keys the
    /// code no longer knows about never reach the database.
    /// </summary>
    Task UpsertAsync(string key, JsonObject data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops the override so the section renders the built-in defaults again.
    /// <c>false</c> when the section had no override to begin with.
    /// </summary>
    Task<bool> ResetAsync(string key, CancellationToken cancellationToken = default);
}

public sealed class SiteContentService(AmtarcDbContext db, ILogger<SiteContentService> logger)
    : ISiteContentService
{
    // camelCase because the stored JSON was written by the previous site with camelCase keys
    // (hero.ctaPrimary.label, announcements.medicalCertificate.badge, ...).
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public async Task<SiteContent> GetAsync(CancellationToken cancellationToken = default)
    {
        var stored = await db.SiteContent
            .AsNoTracking()
            .ToDictionaryAsync(row => row.Key, row => row.Data, cancellationToken);

        return new SiteContent
        {
            Hero = Resolve(stored, SectionKey.Hero, SiteContentDefaults.Hero),
            Announcements = Resolve(stored, SectionKey.Announcements, SiteContentDefaults.Announcements),
            PracticalInfo = Resolve(stored, SectionKey.PracticalInfo, SiteContentDefaults.PracticalInfo),
            Contact = Resolve(stored, SectionKey.Contact, SiteContentDefaults.Contact),
        };
    }

    public async Task<JsonObject> GetSectionAsync(
        string key, CancellationToken cancellationToken = default)
    {
        var defaults = DefaultsNodeFor(key);
        var stored = await db.SiteContent
            .AsNoTracking()
            .Where(row => row.Key == key)
            .Select(row => row.Data)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(stored))
        {
            return defaults;
        }

        try
        {
            return JsonMerge.Merge(defaults, JsonNode.Parse(stored)) as JsonObject ?? defaults;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception,
                "Stored content for section {SectionKey} could not be parsed; using defaults.", key);
            return defaults;
        }
    }

    public async Task UpsertAsync(
        string key, JsonObject data, CancellationToken cancellationToken = default)
    {
        // Merging over the defaults before storing is what keeps the row's shape honest: fields
        // the code has dropped never get written back, and fields the form did not send keep
        // their default rather than disappearing.
        var normalized = JsonMerge.Merge(DefaultsNodeFor(key), data)?.ToJsonString() ?? "{}";

        var row = await db.SiteContent.SingleOrDefaultAsync(r => r.Key == key, cancellationToken);
        if (row is null)
        {
            db.SiteContent.Add(new SiteContentEntry { Key = key, Data = normalized });
        }
        else
        {
            row.Data = normalized;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        var row = await db.SiteContent.SingleOrDefaultAsync(r => r.Key == key, cancellationToken);
        if (row is null)
        {
            return false;
        }

        db.SiteContent.Remove(row);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static JsonObject DefaultsNodeFor(string key)
    {
        object defaults = key switch
        {
            SectionKey.Hero => SiteContentDefaults.Hero,
            SectionKey.Announcements => SiteContentDefaults.Announcements,
            SectionKey.PracticalInfo => SiteContentDefaults.PracticalInfo,
            SectionKey.Contact => SiteContentDefaults.Contact,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown content section."),
        };

        return JsonSerializer.SerializeToNode(defaults, defaults.GetType(), SerializerOptions)
            as JsonObject ?? [];
    }

    private TSection Resolve<TSection>(
        Dictionary<string, string> stored, string key, TSection defaults)
        where TSection : class
    {
        if (!stored.TryGetValue(key, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return defaults;
        }

        try
        {
            var defaultNode = JsonSerializer.SerializeToNode(defaults, SerializerOptions);
            var storedNode = JsonNode.Parse(json);
            var merged = JsonMerge.Merge(defaultNode, storedNode);

            return merged.Deserialize<TSection>(SerializerOptions) ?? defaults;
        }
        catch (JsonException exception)
        {
            // A malformed row must not take the public page down; fall back to the defaults.
            logger.LogWarning(exception,
                "Stored content for section {SectionKey} could not be parsed; using defaults.", key);
            return defaults;
        }
    }
}
