using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);
var azureAdSection = builder.Configuration.GetSection("AzureAd");
var metadataAddress = azureAdSection["MetadataAddress"];
var authority = azureAdSection["Authority"];
var validIssuer = azureAdSection["ValidIssuer"];

// Sem banco de dados: a sessão (claims + tokens) fica só no cookie de autenticação.
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        azureAdSection.Bind(options);
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.MapInboundClaims = false;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        if (!string.IsNullOrWhiteSpace(authority))
        {
            options.Authority = authority;
        }

        if (!string.IsNullOrWhiteSpace(metadataAddress))
        {
            options.MetadataAddress = metadataAddress;
        }

        if (!string.IsNullOrWhiteSpace(validIssuer))
        {
            options.TokenValidationParameters.ValidIssuer = validIssuer;
        }

        options.TokenValidationParameters.NameClaimType = "name";
    });

builder.Services.AddControllersWithViews();

// App Service termina o TLS no front-end; respeita X-Forwarded-Proto para gerar redirect_uri https.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
