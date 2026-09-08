using Amtarc.Web.Infrastructure;
using Microsoft.AspNetCore.OutputCaching;

namespace Amtarc.Web.Services;

public interface IPublicPageCache
{
    /// <summary>Drops the cached public page after a news item changed.</summary>
    Task EvictNewsAsync(CancellationToken cancellationToken = default);

    /// <summary>Drops the cached public page after an editable section changed.</summary>
    Task EvictContentAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Invalidation is done here, inside the services that perform the writes, rather than in each
/// page handler: a write path that forgets to evict produces a stale public page that nothing
/// fails on, and there is exactly one place that knows a write happened.
/// </summary>
public sealed class PublicPageCache(IOutputCacheStore store) : IPublicPageCache
{
    public Task EvictNewsAsync(CancellationToken cancellationToken = default) =>
        store.EvictByTagAsync(OutputCacheSetup.NewsTag, cancellationToken).AsTask();

    public Task EvictContentAsync(CancellationToken cancellationToken = default) =>
        store.EvictByTagAsync(OutputCacheSetup.ContentTag, cancellationToken).AsTask();
}
