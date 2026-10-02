using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sustainsys.Saml2.AspNetCore2;

namespace saml2.Controllers;

[Route("[controller]/[action]")]
public class AccountController : Controller
{
    [AllowAnonymous]
    public IActionResult SignIn(string returnUrl = "/")
    {
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, Saml2Defaults.Scheme);
    }

    [Authorize]
    [ActionName("SignOut")]
    public IActionResult Logout()
    {
        var returnUrl = Url.Action("Index", "Home") ?? "/";

        return SignOut(
            new AuthenticationProperties { RedirectUri = returnUrl },
            CookieAuthenticationDefaults.AuthenticationScheme,
            Saml2Defaults.Scheme);
    }
}