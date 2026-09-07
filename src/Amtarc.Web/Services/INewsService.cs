using Amtarc.Web.Domain;

namespace Amtarc.Web.Services;

/// <summary>The fields an admin fills in; everything else about a news item is derived.</summary>
public sealed record NewsInput
{
    public required string Title { get; init; }

    public required NewsCategory Category { get; init; }

    public string? Excerpt { get; init; }

    public required string Body { get; init; }

    public string? ImageUrl { get; init; }

    public bool Published { get; init; } = true;
}

public interface INewsService
{
    /// <summary>Published items only, newest first (by <see cref="News.PublishedAt"/>).</summary>
    Task<IReadOnlyList<News>> FindPublishedAsync(CancellationToken cancellationToken = default);

    /// <summary>Everything including drafts, newest first — the back-office listing.</summary>
    Task<IReadOnlyList<News>> FindAllAsync(CancellationToken cancellationToken = default);

    Task<News?> FindByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<News> CreateAsync(NewsInput input, CancellationToken cancellationToken = default);

    /// <summary><c>false</c> when no item has that id.</summary>
    Task<bool> UpdateAsync(string id, NewsInput input, CancellationToken cancellationToken = default);

    /// <summary><c>false</c> when no item has that id.</summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// A slug derived from <paramref name="title"/> that is not yet taken, appending
    /// <c>-2</c>, <c>-3</c>, … on collision.
    /// </summary>
    Task<string> GenerateUniqueSlugAsync(string title, CancellationToken cancellationToken = default);
}
