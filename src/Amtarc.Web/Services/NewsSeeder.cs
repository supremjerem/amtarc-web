using Amtarc.Web.Data;
using Amtarc.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace Amtarc.Web.Services;

/// <summary>
/// Seeds the three sample news items outside production, so a fresh checkout has something on
/// the home page. Mirrors the previous <c>prisma/seed.ts</c> content. Only runs when the table
/// is empty — it never touches rows an admin has written.
/// </summary>
public sealed class NewsSeeder(AmtarcDbContext db, ILogger<NewsSeeder> logger)
{
    private static readonly (string Slug, NewsCategory Category, string Title, string Body)[] Items =
    [
        ("concours-interne-fin-de-saison", NewsCategory.Concours,
            "Concours interne de fin de saison",
            "Retrouvez le calendrier des compétitions et les classements du club, mis à jour tout au long de l'année."),
        ("amelioration-continue-du-stand", NewsCategory.Travaux,
            "Amélioration continue du stand",
            "Pas de tir, alvéoles, buttes : les bénévoles entretiennent et modernisent les installations en permanence."),
        ("bourse-aux-armes", NewsCategory.Evenement,
            "Bourse aux armes",
            "Un rendez-vous attendu des passionnés. Achat, vente et échange dans le respect de la réglementation."),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.News.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        for (var i = 0; i < Items.Length; i++)
        {
            var (slug, category, title, body) = Items[i];
            db.News.Add(new News
            {
                Id = Guid.NewGuid().ToString(),
                Slug = slug,
                Category = category,
                Title = title,
                Excerpt = body,
                Body = body,
                Published = true,
                // Stagger so the listing has a deterministic newest-first order.
                PublishedAt = now.AddMinutes(-i),
                CreatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var seeded = Items.Length;
        logger.LogInformation("Seeded {Count} sample news items.", seeded);
    }
}
