using System.Text.Json;
using CustomAuthExtension.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var externalId = builder.Configuration.GetSection("ExternalId");
var tenantId = externalId["TenantId"];
var apiClientId = externalId["ApiClientId"];
var apiIdentifierUri = externalId["ApiIdentifierUri"];
var authEventsAppId = externalId["AuthenticationEventsAppId"] ?? "99045fe1-7639-4a75-9d4a-577b6ca3810f";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // O token que o Entra envia para a extensão é emitido pelo tenant External ID e assinado com
        // chaves publicadas só no metadata do ciamlogin.com (o de login.microsoftonline.com não as tem).
        options.Authority = $"https://{tenantId}.ciamlogin.com/{tenantId}/v2.0";
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidIssuers =
        [
            $"https://login.microsoftonline.com/{tenantId}/v2.0",
            $"https://{tenantId}.ciamlogin.com/{tenantId}/v2.0",
            $"https://sts.windows.net/{tenantId}/"
        ];
        options.TokenValidationParameters.ValidAudiences =
            new[] { apiClientId, apiIdentifierUri }.Where(v => !string.IsNullOrWhiteSpace(v)).Cast<string>().ToArray();
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Auth").LogWarning("Token rejeitado: {Error}", ctx.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Só aceita chamadas do serviço "Azure AD Authentication Events" (azp no token v2, appid no v1).
    options.AddPolicy("EntraAuthenticationEvents", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(ctx =>
            ctx.User.HasClaim("azp", authEventsAppId) || ctx.User.HasClaim("appid", authEventsAppId)));
});

var app = builder.Build();

// O Entra espera no máximo 2s pela resposta: baixa o metadata/JWKS já no startup e não na 1ª chamada.
app.Lifetime.ApplicationStarted.Register(() => _ = Task.Run(async () =>
{
    var jwtOptions = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
    await jwtOptions.ConfigurationManager!.GetConfigurationAsync(CancellationToken.None);
    app.Logger.LogInformation("Metadata OIDC do External ID carregado ({Authority})", jwtOptions.Authority);
}));

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "CustomAuthExtension.Api", endpoint = "/api/token-issuance-start" }));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTimeOffset.UtcNow }));

app.MapPost("/api/token-issuance-start", (TokenIssuanceStartRequest request, ILogger<Program> logger) =>
{
    var context = request.Data?.AuthenticationContext;
    var user = context?.User;

    logger.LogInformation(
        "OnTokenIssuanceStart recebido: correlationId={CorrelationId} app={App} user={UserId} ({UserName}) ip={Ip} payload={Payload}",
        context?.CorrelationId,
        context?.ClientServicePrincipal?.AppDisplayName,
        user?.Id,
        user?.DisplayName,
        context?.Client?.Ip,
        JsonSerializer.Serialize(request));

    var claims = DemoClaims.Build(context);

    logger.LogInformation("Claims devolvidas para o token: {Claims}", JsonSerializer.Serialize(claims));

    return Results.Ok(new TokenIssuanceStartResponse(
        new TokenIssuanceStartResponseData([new ProvideClaimsForTokenAction(claims)])));
})
.RequireAuthorization("EntraAuthenticationEvents");

app.Run();
