using Amtarc.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Amtarc.Web.Pages.Admin.News;

public class CreateModel(INewsService news, IUploadService uploads) : NewsFormPageModel(uploads)
{
    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await StoreImageIfPickedAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var created = await news.CreateAsync(Input.ToNewsInput(), cancellationToken);

        StatusMessage = $"« {created.Title} » créée.";
        return RedirectToPage("Index");
    }
}
