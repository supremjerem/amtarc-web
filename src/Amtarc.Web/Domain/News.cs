namespace Amtarc.Web.Domain;

/// <summary>A news item. Public listing shows <see cref="Published"/> items, newest first.</summary>
public class News : ITimestamped
{
    public string Id { get; set; } = string.Empty;

    /// <summary>URL-safe handle derived from the title, unique across all items.</summary>
    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public NewsCategory Category { get; set; }

    public string? ImageUrl { get; set; }

    public string? Excerpt { get; set; }

    public string Body { get; set; } = string.Empty;

    public bool Published { get; set; } = true;

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
