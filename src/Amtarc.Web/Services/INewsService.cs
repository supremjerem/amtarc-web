using Amtarc.Web.Domain;

namespace Amtarc.Web.Services;

public interface INewsService
{
    /// <summary>Published items only, newest first (by <see cref="News.PublishedAt"/>).</summary>
    Task<IReadOnlyList<News>> FindPublishedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// A slug derived from <paramref name="title"/> that is not yet taken, appending
    /// <c>-2</c>, <c>-3</c>, … on collision.
    /// </summary>
    Task<string> GenerateUniqueSlugAsync(string title, CancellationToken cancellationToken = default);
}
