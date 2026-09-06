using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Amtarc.Web.Services;

/// <summary>
/// Turns a title into a URL-safe slug. Port of the previous NestJS <c>slugify</c>:
/// strip diacritics, lowercase, then collapse every run of non-alphanumeric characters
/// into a single hyphen.
/// </summary>
public static partial class SlugGenerator
{
    public static string Slugify(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        var decomposed = title.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        var stripped = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var parts = NonAlphanumeric().Split(stripped).Where(p => p.Length > 0);
        return string.Join('-', parts);
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
