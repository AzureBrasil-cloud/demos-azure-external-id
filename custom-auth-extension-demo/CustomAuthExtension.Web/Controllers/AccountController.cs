using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomAuthExtension.Web.Controllers;

[Route("[controller]/[action]")]
public class AccountController : Controller
{
    [AllowAnonymous]
    public IActionResult SignIn(string returnUrl = "/Home/Token")
    {
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Authorize]
    [ActionName("SignOut")]
    public IActionResult Logout()
    {
        var returnUrl = Url.Action("Index", "Home") ?? "/";

        return SignOut(
            new AuthenticationProperties { RedirectUri = returnUrl },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
