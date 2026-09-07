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

    public async Task<IReadOnlyList<News>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        // Ordered by creation, not publication: a draft's publication date is not meaningful yet,
        // and what the admin wants to find is what they wrote last.
        return await _db.News
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<News?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _db.News
            .AsNoTracking()
            .SingleOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<News> CreateAsync(NewsInput input, CancellationToken cancellationToken = default)
    {
        var item = new News
        {
            Id = Guid.NewGuid().ToString(),
            Slug = await GenerateUniqueSlugAsync(input.Title, cancellationToken),
            Title = input.Title,
            Category = input.Category,
            Excerpt = input.Excerpt,
            Body = input.Body,
            ImageUrl = input.ImageUrl,
            Published = input.Published,
        };

        // PublishedAt and CreatedAt are left unset so the column defaults fill them — what
        // Prisma's @default(now()) did.
        _db.News.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return item;
    }

    public async Task<bool> UpdateAsync(
        string id, NewsInput input, CancellationToken cancellationToken = default)
    {
        var item = await _db.News.SingleOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        // The slug is deliberately not regenerated from an edited title: it is the item's public
        // handle, and rewriting it would silently break any link already shared.
        item.Title = input.Title;
        item.Category = input.Category;
        item.Excerpt = input.Excerpt;
        item.Body = input.Body;
        item.ImageUrl = input.ImageUrl;
        item.Published = input.Published;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await _db.News.SingleOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        _db.News.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
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
