using Amtarc.Web.Data;
using Amtarc.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace Amtarc.Web.Services;

public sealed class NewsService(AmtarcDbContext db) : INewsService
{
    private readonly AmtarcDbContext _db = db;

    public async Task<IReadOnlyList<News>> FindPublishedAsync(CancellationToken cancellationToken = default)
    {
        return await _db.News
            .Where(n => n.Published)
            .OrderByDescending(n => n.PublishedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateUniqueSlugAsync(string title, CancellationToken cancellationToken = default)
    {
        var baseSlug = SlugGenerator.Slugify(title);
        var slug = baseSlug;
        var suffix = 2;

        while (await _db.News.AnyAsync(n => n.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }
}
