using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using saml2.Models;

namespace saml2.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public IActionResult Privacy()
    {
        var model = new ProtectedViewModel
        {
            Claims = User.Claims
                .Select(claim => new ClaimEntry(claim.Type, claim.Value))
                .ToList()
        };

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
