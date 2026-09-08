using System.Diagnostics.CodeAnalysis;
using Amtarc.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Amtarc.Web.Data;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build the model without spinning up the web
/// host (which migrates + seeds on start). The connection string here is only used when a
/// command actually talks to a database; <c>migrations add</c> / <c>script</c> do not.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Only ever invoked by the dotnet-ef tooling.")]
public sealed class AmtarcDbContextFactory : IDesignTimeDbContextFactory<AmtarcDbContext>
{
    public AmtarcDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Amtarc")
            ?? "Host=localhost;Port=5434;Database=amtarc;Username=amtarc;Password=amtarc";

        var options = new DbContextOptionsBuilder<AmtarcDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MapEnum<NewsCategory>("NewsCategory"))
            .Options;

        return new AmtarcDbContext(options);
    }
}
