using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CustomAuthExtension.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CustomAuthExtension.Web.Controllers;

public class HomeController : Controller
{
    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Token()
    {
        var auth = await HttpContext.AuthenticateAsync();
        var idToken = auth.Properties?.GetTokenValue("id_token");

        string? headerJson = null, payloadJson = null, issuer = null, audience = null;
        DateTimeOffset? issuedAt = null, expiresAt = null;

        if (!string.IsNullOrWhiteSpace(idToken))
        {
            var jwt = new JsonWebTokenHandler().ReadJsonWebToken(idToken);
            headerJson = PrettyPrint(jwt.EncodedHeader);
            payloadJson = PrettyPrint(jwt.EncodedPayload);
            issuer = jwt.Issuer;
            audience = string.Join(", ", jwt.Audiences);
            issuedAt = jwt.IssuedAt == DateTime.MinValue ? null : new DateTimeOffset(jwt.IssuedAt, TimeSpan.Zero);
            expiresAt = jwt.ValidTo == DateTime.MinValue ? null : new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero);
        }

        var model = new TokenViewModel
        {
            IdToken = idToken,
            HeaderJson = headerJson,
            PayloadJson = payloadJson,
            Issuer = issuer,
            Audience = audience,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            CookieExpiresAt = auth.Properties?.ExpiresUtc?.ToString("u"),
            Claims = User.Claims
                .Select(c => new ClaimEntry(c.Type, c.Value, ExtensionClaimTypes.Contains(c.Type)))
                .OrderByDescending(c => c.FromExtension)
                .ToList()
        };

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static string PrettyPrint(string base64Url)
    {
        var json = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(base64Url));
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement, PrettyJson);
    }
}
