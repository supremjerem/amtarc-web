using Amtarc.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Amtarc.Web.Pages.Admin;

/// <summary>
/// Sign-out is POST-only and anti-forgery protected (Razor Pages does that automatically), so a
/// third-party page cannot sign the admin out with an image tag. A GET lands on the login page.
/// </summary>
[AllowAnonymous]
public class LogoutModel : PageModel
{
    public IActionResult OnGet() => Redirect(AuthenticationSetup.LoginPath);

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(AuthenticationSetup.Scheme);
        return Redirect(AuthenticationSetup.LoginPath);
    }
}
