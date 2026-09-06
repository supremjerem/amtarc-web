using Amtarc.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amtarc.Web.Data.Configurations;

public sealed class AdminConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> builder)
    {
        builder.ToTable("Admin");

        builder.HasKey(a => a.Id).HasName("Admin_pkey");
        builder.Property(a => a.Id).HasColumnName("id").HasColumnType("text").ValueGeneratedNever();

        builder.Property(a => a.Email).HasColumnName("email").HasColumnType("text").IsRequired();
        builder.Property(a => a.PasswordHash).HasColumnName("passwordHash").HasColumnType("text").IsRequired();
        builder.Property(a => a.Name).HasColumnName("name").HasColumnType("text");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("createdAt")
            .HasColumnType("timestamp(3) without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.HasIndex(a => a.Email).IsUnique().HasDatabaseName("Admin_email_key");
    }
}
