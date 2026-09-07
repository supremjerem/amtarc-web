using Amtarc.Web.Services;
using Amtarc.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Amtarc.Web.Pages.Admin;

/// <summary>
/// What the create and edit screens share: the bound form, and turning a chosen file into a
/// stored image URL.
/// </summary>
public abstract class NewsFormPageModel(IUploadService uploads) : PageModel
{
    [BindProperty]
    public NewsFormInput Input { get; set; } = new();

    /// <summary>The file the admin picked, if any. Optional — an item need not have an image.</summary>
    [BindProperty]
    public IFormFile? Image { get; set; }

    /// <summary>
    /// Stores the picked file and points <see cref="Input"/> at it. Runs before the rest of the
    /// validation so that a rejected title does not also throw away the upload — a browser cannot
    /// re-populate a file input, so the admin would otherwise have to pick the file again.
    /// </summary>
    protected async Task StoreImageIfPickedAsync(CancellationToken cancellationToken)
    {
        if (Image is null || Image.Length == 0)
        {
            return;
        }

        await using var stream = Image.OpenReadStream();
        var result = await uploads.SaveAsync(stream, Image.Length, cancellationToken);

        if (result.Succeeded)
        {
            Input.ImageUrl = result.Url;

            // The URL just changed under the model binder; drop the value it validated so the
            // fresh one is what gets checked and redisplayed.
            ModelState.Remove($"{nameof(Input)}.{nameof(Input.ImageUrl)}");
            TryValidateModel(Input, nameof(Input));
        }
        else
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.ImageUrl)}", result.Error!);
        }
    }
}
