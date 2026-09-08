using Amtarc.Web.Domain;
using Amtarc.Web.Infrastructure;
using Amtarc.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace Amtarc.Web.Pages;

/// <summary>
/// The whole public site: one scrolling page composed of eight anchored sections.
/// Editable copy comes from <see cref="ISiteContentService"/> (stored JSON merged over the
/// built-in defaults); the rest is code-only content.
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPagePolicy)]
public class IndexModel(ISiteContentService siteContent, INewsService news) : PageModel
{
    public SiteContent Sections { get; private set; } = null!;

    public IReadOnlyList<News> News { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Sections = await siteContent.GetAsync(cancellationToken);
        News = await news.FindPublishedAsync(cancellationToken);
    }
}
