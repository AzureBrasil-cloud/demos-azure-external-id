using Microsoft.AspNetCore.Authentication.Cookies;
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Metadata;

var builder = WebApplication.CreateBuilder(args);
var saml2Section = builder.Configuration.GetSection("Saml2");

// Add services to the container.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = Saml2Defaults.Scheme;
    })
    .AddCookie()
    .AddSaml2(options =>
    {
        var entityId = saml2Section["EntityId"]
            ?? throw new InvalidOperationException("Saml2:EntityId is required.");
        var identityProviderEntityId = saml2Section["IdentityProviderEntityId"]
            ?? throw new InvalidOperationException("Saml2:IdentityProviderEntityId is required.");
        var metadataUrl = saml2Section["MetadataUrl"]
            ?? throw new InvalidOperationException("Saml2:MetadataUrl is required.");

        options.SPOptions.EntityId = new EntityId(entityId);

        var modulePath = saml2Section["ModulePath"];
        if (!string.IsNullOrWhiteSpace(modulePath))
        {
            options.SPOptions.ModulePath = modulePath;
        }

        var returnUrl = saml2Section["ReturnUrl"];
        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var returnUri))
        {
            options.SPOptions.ReturnUrl = returnUri;
        }

        var identityProvider = new IdentityProvider(new EntityId(identityProviderEntityId), options.SPOptions)
        {
            MetadataLocation = metadataUrl,
            LoadMetadata = true,
            AllowUnsolicitedAuthnResponse = true
        };

        options.IdentityProviders.Add(identityProvider);
    });

builder.Services.AddControllersWithViews();

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
