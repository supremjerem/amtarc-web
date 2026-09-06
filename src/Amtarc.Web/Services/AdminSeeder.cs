using Amtarc.Web.Data;
using Amtarc.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace Amtarc.Web.Services;

/// <summary>
/// Ensures the single back-office account exists and its password hash matches
/// <c>Admin:Password</c>. Runs on every startup, so rotating the password is: change the
/// config value and restart. Parity with the previous <c>prisma/seed.ts</c> behaviour.
/// Full "refuse to boot on a missing / weak password" validation arrives with the Options
/// hardening phase; here a missing value is simply skipped.
/// </summary>
public sealed class AdminSeeder(
    AmtarcDbContext db,
    IConfiguration configuration,
    ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Admin:Email / Admin:Password not configured — skipping admin seed.");
            return;
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);
        var admin = await db.Admins.SingleOrDefaultAsync(a => a.Email == email, cancellationToken);

        if (admin is null)
        {
            db.Admins.Add(new Admin
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                PasswordHash = hash,
                Name = "Admin AMTARC",
            });
        }
        else
        {
            admin.PasswordHash = hash;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
