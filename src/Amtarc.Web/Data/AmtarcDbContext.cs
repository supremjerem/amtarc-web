using Amtarc.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace Amtarc.Web.Data;

public class AmtarcDbContext(DbContextOptions<AmtarcDbContext> options) : DbContext(options)
{
    public DbSet<News> News => Set<News>();

    public DbSet<SiteContentEntry> SiteContent => Set<SiteContentEntry>();

    public DbSet<Admin> Admins => Set<Admin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AmtarcDbContext).Assembly);
    }
}
