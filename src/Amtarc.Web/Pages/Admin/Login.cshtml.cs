using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Amtarc.Web.Security;
using Amtarc.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace Amtarc.Web.Pages.Admin;

[AllowAnonymous]
[EnableRateLimiting(AuthenticationSetup.LoginRateLimitPolicy)]
public class LoginModel(IAuthService auth, ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Set when the rate limiter bounced a sign-in attempt back here. Shown as its own message:
    /// "wrong password" and "too many attempts" must not look the same to the person typing.
    /// </summary>
    [BindProperty(SupportsGet = true, Name = "lockout")]
    public bool LockedOut { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        // Already signed in — no reason to show the form again.
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(SafeReturnUrl(returnUrl));
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var admin = await auth.ValidateAsync(Input.Email, Input.Password, cancellationToken);

        if (admin is null)
        {
            // One message for both "unknown email" and "wrong password" — anything finer grained
            // tells an attacker which half they got right.
            logger.LogWarning("Rejected admin sign-in attempt for {Email}.", Input.Email);
            ModelState.AddModelError(string.Empty, "Identifiants invalides.");
            return Page();
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, admin.Id),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Name, admin.Name ?? admin.Email),
                new Claim(AuthenticationSetup.RoleClaim, AuthenticationSetup.RoleClaimValue),
            ],
            AuthenticationSetup.Scheme);

        await HttpContext.SignInAsync(
            AuthenticationSetup.Scheme,
            new ClaimsPrincipal(identity),
            // Session cookie: closing the browser signs the admin out.
            new AuthenticationProperties { IsPersistent = false });

        logger.LogInformation("Admin {Email} signed in.", admin.Email);
        return Redirect(SafeReturnUrl(returnUrl));
    }

    /// <summary>
    /// Only ever redirect within this site — a returnUrl is attacker-controllable, and an open
    /// redirect off the login page is a phishing primitive.
    /// </summary>
    private string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : AuthenticationSetup.LandingPath;

    public sealed class InputModel
    {
        [Required(ErrorMessage = "L'adresse e-mail est requise.")]
        [EmailAddress(ErrorMessage = "Adresse e-mail invalide.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis.")]
        public string Password { get; set; } = string.Empty;
    }
}
