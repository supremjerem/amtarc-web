using Amtarc.Web.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Amtarc.Web.Pages.Admin.Content;

public class IndexModel : PageModel
{
    public IReadOnlyList<SectionConfig> Sections => SectionFieldConfig.All;

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
    }
}
