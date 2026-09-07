using Amtarc.Web.Data;
using Amtarc.Web.Domain;
using Amtarc.Web.Security;
using Microsoft.EntityFrameworkCore;

namespace Amtarc.Web.Services;

public interface IAuthService
{
    /// <summary>
    /// The matching admin when the credentials are valid, otherwise <c>null</c>. Callers must
    /// report both failure modes identically — a distinguishable "unknown email" answer would
    /// let anyone enumerate the account.
    /// </summary>
    Task<Admin?> ValidateAsync(string email, string password, CancellationToken cancellationToken = default);
}

public sealed class AuthService(AmtarcDbContext db, IAdminPasswordHasher hasher) : IAuthService
{
    public async Task<Admin?> ValidateAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        var admin = await db.Admins
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Email == email, cancellationToken);

        if (admin is null)
        {
            // Still hash something so an unknown email does not return measurably faster than a
            // known one with the wrong password.
            hasher.Verify(password, "$2a$10$............................................................");
            return null;
        }

        return hasher.Verify(password, admin.PasswordHash) ? admin : null;
    }
}
