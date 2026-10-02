using CustomAuthExtension.Functions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// App settings no formato ExternalId__TenantId (Azure / local.settings.json).
var externalId = builder.Configuration.GetSection("ExternalId");
builder.Services.AddSingleton(new ExternalIdOptions
{
    TenantId = externalId["TenantId"] ?? throw new InvalidOperationException("App setting ExternalId__TenantId não configurado."),
    ApiClientId = externalId["ApiClientId"],
    ApiIdentifierUri = externalId["ApiIdentifierUri"],
    ApiClientSecret = externalId["ApiClientSecret"],
    AuthenticationEventsAppId = externalId["AuthenticationEventsAppId"] ?? "99045fe1-7639-4a75-9d4a-577b6ca3810f"
});
builder.Services.AddSingleton<EntraEventTokenValidator>();
builder.Services.AddHostedService<OidcMetadataWarmup>();

builder.Build().Run();
