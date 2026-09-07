namespace Amtarc.Web.Security;

/// <summary>
/// bcrypt hashing for the single back-office account. Work factor 10 matches the hashes the
/// previous site wrote, so a database restored from production verifies without a reset.
/// </summary>
public sealed class AdminPasswordHasher : IAdminPasswordHasher
{
    private const int WorkFactor = 10;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        // A row whose hash predates bcrypt or was truncated must deny the sign-in, not throw a 500
        // out of the login handler. BCrypt.Net signals a malformed hash three different ways
        // depending on where the parse gives up.
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

public interface IAdminPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
