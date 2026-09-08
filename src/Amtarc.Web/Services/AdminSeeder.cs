using Amtarc.Web.Data;
using Amtarc.Web.Domain;
using Amtarc.Web.Options;
using Amtarc.Web.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amtarc.Web.Services;

/// <summary>
/// Ensures the single back-office account exists and its password hash matches
/// <see cref="AdminOptions.Password"/>. Runs on every startup, so rotating the password is:
/// change the config value and restart. Parity with the previous <c>prisma/seed.ts</c>.
/// The options are validated at startup, so by the time this runs the values are known good.
/// </summary>
public sealed class AdminSeeder(
    AmtarcDbContext db,
    IAdminPasswordHasher hasher,
    IOptions<AdminOptions> options,
    ILogger<AdminSeeder> logger)
{
    private readonly AdminOptions _options = options.Value;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var hash = hasher.Hash(_options.Password);
        var admin = await db.Admins
            .SingleOrDefaultAsync(a => a.Email == _options.Email, cancellationToken);

        if (admin is null)
        {
            db.Admins.Add(new Admin
            {
                Id = Guid.NewGuid().ToString(),
                Email = _options.Email,
                PasswordHash = hash,
                Name = _options.DisplayName,
            });

            logger.LogInformation("Created the admin account for {Email}.", _options.Email);
        }
        else
        {
            admin.PasswordHash = hash;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
