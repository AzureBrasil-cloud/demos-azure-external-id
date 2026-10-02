using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);
var azureAdSection = builder.Configuration.GetSection("AzureAd");
var metadataAddress = azureAdSection["MetadataAddress"];
var authority = azureAdSection["Authority"];
var validIssuer = azureAdSection["ValidIssuer"];

// Add services to the container.
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        azureAdSection.Bind(options);
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
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
            // External ID user flow discovery endpoint (includes appid query string).
            options.MetadataAddress = metadataAddress;
        }

        if (!string.IsNullOrWhiteSpace(validIssuer))
        {
            options.TokenValidationParameters.ValidIssuer = validIssuer;
        }

        options.TokenValidationParameters.NameClaimType = "name";
    });

builder.Services.AddControllersWithViews();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("FuncionarioInternoOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Profile", "funcionario-atrio");
    });

    options.AddPolicy("InvestidorOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Profile", "Investidor");
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
