using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Amtarc.Web.Pages.Admin.News;

public class IndexModel : PageModel
{
    public string AdminEmail { get; private set; } = string.Empty;

    public void OnGet() =>
        AdminEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
}
