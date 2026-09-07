using Amtarc.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Amtarc.Web.Pages.Admin.News;

public class IndexModel(INewsService news) : PageModel
{
    public IReadOnlyList<Web.Domain.News> Items { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Items = await news.FindAllAsync(cancellationToken);

    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken cancellationToken)
    {
        StatusMessage = await news.DeleteAsync(id, cancellationToken)
            ? "Actualité supprimée."
            : "Cette actualité n'existe plus.";

        return RedirectToPage();
    }
}
