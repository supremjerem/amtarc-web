using System.ComponentModel.DataAnnotations;

namespace Amtarc.Web.Options;

/// <summary>
/// Rejects the values a deployment ends up with when nobody replaced the example config. A
/// back-office anyone can log into is the realistic consequence, and refusing to boot is the only
/// check that cannot be skipped by not reading the README. Port of the previous site's Zod rule.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotAPlaceholderAttribute : ValidationAttribute
{
    private static readonly HashSet<string> Placeholders = new(StringComparer.OrdinalIgnoreCase)
    {
        "change-me", "changeme", "change_me", "secret", "password", "admin",
    };

    public NotAPlaceholderAttribute()
        : base("must not be left at its placeholder value")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string candidate || !Placeholders.Contains(candidate.Trim());
}
