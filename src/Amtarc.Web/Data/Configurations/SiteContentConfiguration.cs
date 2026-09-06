using Amtarc.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amtarc.Web.Data.Configurations;

public sealed class SiteContentConfiguration : IEntityTypeConfiguration<SiteContentEntry>
{
    public void Configure(EntityTypeBuilder<SiteContentEntry> builder)
    {
        builder.ToTable("SiteContent");

        builder.HasKey(s => s.Key).HasName("SiteContent_pkey");
        builder.Property(s => s.Key).HasColumnName("key").HasColumnType("text").ValueGeneratedNever();

        builder.Property(s => s.Data).HasColumnName("data").HasColumnType("jsonb").IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updatedAt")
            .HasColumnType("timestamp(3) without time zone")
            .IsRequired();
    }
}
