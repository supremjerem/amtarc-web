using Amtarc.Web.Services;
using Amtarc.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Amtarc.Web.Pages.Admin.News;

public class EditModel(INewsService news, IUploadService uploads) : NewsFormPageModel(uploads)
{
    public string Id { get; private set; } = string.Empty;

    /// <summary>Shown as read-only context: the slug is the public handle and never changes.</summary>
    public string Slug { get; private set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        var item = await news.FindByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        Id = item.Id;
        Slug = item.Slug;
        Input = NewsFormInput.From(item);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id, CancellationToken cancellationToken)
    {
        await StoreImageIfPickedAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            var current = await news.FindByIdAsync(id, cancellationToken);
            if (current is null)
            {
                return NotFound();
            }

            Id = current.Id;
            Slug = current.Slug;
            return Page();
        }

        if (!await news.UpdateAsync(id, Input.ToNewsInput(), cancellationToken))
        {
            return NotFound();
        }

        StatusMessage = $"« {Input.Title} » enregistrée.";
        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken cancellationToken)
    {
        StatusMessage = await news.DeleteAsync(id, cancellationToken)
            ? "Actualité supprimée."
            : "Cette actualité n'existe plus.";

        return RedirectToPage("Index");
    }
}
