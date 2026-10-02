using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using teste_b2c.Models;

namespace teste_b2c.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Privacy()
    {
        var idToken = await HttpContext.GetTokenAsync("id_token");

        var model = new ProtectedViewModel
        {
            Claims = User.Claims
                .Select(claim => new ClaimEntry(claim.Type, claim.Value))
                .ToList(),
            IdToken = idToken
        };

        return View(model);
    }

    [Authorize(Policy = "FuncionarioInternoOnly")]
    public IActionResult FuncionarioInterno()
    {
        return View();
    }

    [Authorize(Policy = "InvestidorOnly")]
    public IActionResult Investidor()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
