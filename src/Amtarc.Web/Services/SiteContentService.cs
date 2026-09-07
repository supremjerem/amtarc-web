using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Amtarc.Web.Content;
using Amtarc.Web.Data;
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
