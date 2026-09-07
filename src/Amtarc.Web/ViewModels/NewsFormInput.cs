using System.ComponentModel.DataAnnotations;
using Amtarc.Web.Domain;
using Amtarc.Web.Services;

namespace Amtarc.Web.ViewModels;

/// <summary>
/// What the create and edit forms bind. The rules mirror the previous site's
/// <c>CreateNewsDto</c>, so content that was valid there stays valid here.
/// </summary>
public sealed class NewsFormInput
{
    [Required(ErrorMessage = "Le titre est requis.")]
    [MinLength(3, ErrorMessage = "Le titre doit faire au moins 3 caractères.")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(NewsCategory), ErrorMessage = "Catégorie inconnue.")]
    public NewsCategory Category { get; set; } = NewsCategory.Concours;

    public string? Excerpt { get; set; }

    [Required(ErrorMessage = "Le texte est requis.")]
    [MinLength(1)]
    public string Body { get; set; } = string.Empty;

    [ImageUrl]
    public string? ImageUrl { get; set; }

    public bool Published { get; set; } = true;

    /// <summary>
    /// Empty optional fields are stored as <c>null</c> rather than <c>""</c>, so "no excerpt"
    /// looks the same whether it was never filled in or was cleared.
    /// </summary>
    public NewsInput ToNewsInput() => new()
    {
        Title = Title.Trim(),
        Category = Category,
        Excerpt = NullIfBlank(Excerpt),
        Body = Body.Trim(),
        ImageUrl = NullIfBlank(ImageUrl),
        Published = Published,
    };

    public static NewsFormInput From(Domain.News item) => new()
    {
        Title = item.Title,
        Category = item.Category,
        Excerpt = item.Excerpt,
        Body = item.Body,
        ImageUrl = item.ImageUrl,
        Published = item.Published,
    };

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>
/// Accepts an absolute http(s) URL or a site-relative path. A plain <c>[Url]</c> would reject
/// <c>/uploads/….jpg</c>, which is exactly what this site's own upload handler returns when it is
/// serving images from its own origin.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ImageUrlAttribute : ValidationAttribute
{
    public ImageUrlAttribute()
        : base("L'adresse de l'image est invalide.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is not string candidate || string.IsNullOrWhiteSpace(candidate))
        {
            return true;
        }

        if (candidate.StartsWith('/') && !candidate.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
