using Amtarc.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Amtarc.Web.Tests.Integration;

/// <summary>
/// Drives the real app against a throwaway PostgreSQL container. The app migrates and seeds
/// on startup, so by the time a test gets a client the schema and the admin row exist.
/// </summary>
public sealed class AmtarcWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@amtarc.test";
    public const string AdminPassword = "integration-test-password";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    /// <summary>Uploads go to a throwaway directory, not into the working tree.</summary>
    private readonly string _uploadsDirectory =
        Path.Combine(Path.GetTempPath(), $"amtarc-test-uploads-{Guid.NewGuid():N}");

    public async Task InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();

        if (Directory.Exists(_uploadsDirectory))
        {
            Directory.Delete(_uploadsDirectory, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Amtarc", _postgres.GetConnectionString());
        builder.UseSetting("Admin:Email", AdminEmail);
        builder.UseSetting("Admin:Password", AdminPassword);
        builder.UseSetting("Upload:Directory", _uploadsDirectory);

        // Every test in the collection signs in from the same address, so the production throttle
        // would lock the suite out partway through. The lockout itself is tested on its own host
        // with a deliberately tiny limit.
        builder.UseSetting("Security:LoginAttemptLimit", "10000");
    }

    /// <summary>A fresh scope + <see cref="AmtarcDbContext"/> for arranging or asserting DB state.</summary>
    public AsyncServiceScope CreateScope() => Services.CreateAsyncScope();
}

[CollectionDefinition(Name)]
public sealed class AmtarcWebCollection : ICollectionFixture<AmtarcWebFactory>
{
    public const string Name = "amtarc-web";
}
