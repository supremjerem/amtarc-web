namespace Amtarc.Web.Domain;

/// <summary>The single back-office account. Seeded / rotated from configuration on startup.</summary>
public class Admin
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? Name { get; set; }

    public DateTime CreatedAt { get; set; }
}
