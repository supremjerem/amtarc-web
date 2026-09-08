using System.ComponentModel.DataAnnotations;

namespace Amtarc.Web.Options;

/// <summary>
/// The single back-office account. The password guards the only way into the admin, so it gets
/// the same treatment as a secret rather than a "not empty" check.
/// </summary>
public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>12 characters, matching the previous site's minimum.</summary>
    public const int MinimumPasswordLength = 12;

    [Required(ErrorMessage = "Admin:Email is required.")]
    [EmailAddress(ErrorMessage = "Admin:Email must be a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin:Password is required.")]
    [MinLength(MinimumPasswordLength,
        ErrorMessage = "Admin:Password must be at least 12 characters — run: openssl rand -base64 32")]
    [NotAPlaceholder]
    public string Password { get; set; } = string.Empty;

    public string DisplayName { get; set; } = "Admin AMTARC";
}
