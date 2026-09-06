using Amtarc.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amtarc.Web.Data.Configurations;

/// <summary>
/// Maps <see cref="News"/> onto the table shape carried over from the previous Prisma schema:
/// PascalCase table, camelCase columns, <c>text</c> ids, <c>timestamp(3) without time zone</c>,
/// and the native <c>"NewsCategory"</c> enum.
/// </summary>
public sealed class NewsConfiguration : IEntityTypeConfiguration<News>
{
    public void Configure(EntityTypeBuilder<News> builder)
    {
        builder.ToTable("News");

        builder.HasKey(n => n.Id).HasName("News_pkey");
        builder.Property(n => n.Id).HasColumnName("id").HasColumnType("text").ValueGeneratedNever();

        builder.Property(n => n.Slug).HasColumnName("slug").HasColumnType("text").IsRequired();
        builder.Property(n => n.Title).HasColumnName("title").HasColumnType("text").IsRequired();
        builder.Property(n => n.Category).HasColumnName("category").IsRequired();
        builder.Property(n => n.ImageUrl).HasColumnName("imageUrl").HasColumnType("text");
        builder.Property(n => n.Excerpt).HasColumnName("excerpt").HasColumnType("text");
        builder.Property(n => n.Body).HasColumnName("body").HasColumnType("text").IsRequired();

        builder.Property(n => n.Published)
            .HasColumnName("published")
            .HasColumnType("boolean")
            .HasDefaultValue(true);

        builder.Property(n => n.PublishedAt)
            .HasColumnName("publishedAt")
            .HasColumnType("timestamp(3) without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(n => n.CreatedAt)
            .HasColumnName("createdAt")
            .HasColumnType("timestamp(3) without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(n => n.UpdatedAt)
            .HasColumnName("updatedAt")
            .HasColumnType("timestamp(3) without time zone")
            .IsRequired();

        builder.HasIndex(n => n.Slug).IsUnique().HasDatabaseName("News_slug_key");
        builder.HasIndex(n => new { n.Published, n.PublishedAt })
            .HasDatabaseName("News_published_publishedAt_idx");
    }
}
