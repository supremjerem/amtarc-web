using Amtarc.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Amtarc.Web.Pages.Admin;

/// <summary>The back-office root has no dashboard of its own; news is the landing screen.</summary>
public class IndexModel : PageModel
{
    public IActionResult OnGet() => Redirect(AuthenticationSetup.LandingPath);
}
