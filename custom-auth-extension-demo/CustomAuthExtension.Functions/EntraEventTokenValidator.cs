using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using HttpRequestData = Microsoft.Azure.Functions.Worker.Http.HttpRequestData;

namespace CustomAuthExtension.Functions;

public sealed class ExternalIdOptions
{
    public required string TenantId { get; init; }
    public string? ApiClientId { get; init; }
    public string? ApiIdentifierUri { get; init; }
    // Não é usado para validar o token da extensão; fica disponível se a função precisar de tokens próprios (ex.: Graph).
    public string? ApiClientSecret { get; init; }
    public required string AuthenticationEventsAppId { get; init; }
}

// Sem a integração ASP.NET Core (incompatível com Native AOT) não há middleware JwtBearer,
// então o Bearer token do Entra é validado aqui, com as mesmas regras da Minimal API.
public sealed class EntraEventTokenValidator
{
    private readonly JsonWebTokenHandler _handler = new() { MapInboundClaims = false };
    private readonly TokenValidationParameters _parameters;
    private readonly string _authEventsAppId;
    private readonly ILogger<EntraEventTokenValidator> _logger;

    public EntraEventTokenValidator(ExternalIdOptions options, ILogger<EntraEventTokenValidator> logger)
    {
        _logger = logger;
        _authEventsAppId = options.AuthenticationEventsAppId;

        // O token que o Entra envia para a extensão é emitido pelo tenant External ID e assinado com
        // chaves publicadas só no metadata do ciamlogin.com (o de login.microsoftonline.com não as tem).
        Authority = $"https://{options.TenantId}.ciamlogin.com/{options.TenantId}/v2.0";
        ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{Authority}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });

        _parameters = new TokenValidationParameters
        {
            // Busca as chaves de assinatura no metadata e faz refresh se aparecer um kid novo.
            ConfigurationManager = ConfigurationManager,
            ValidIssuers =
            [
                $"https://login.microsoftonline.com/{options.TenantId}/v2.0",
                $"https://{options.TenantId}.ciamlogin.com/{options.TenantId}/v2.0",
                $"https://sts.windows.net/{options.TenantId}/"
            ],
            ValidAudiences = new[] { options.ApiClientId, options.ApiIdentifierUri }
                .Where(v => !string.IsNullOrWhiteSpace(v)).Cast<string>().ToArray()
        };
    }

    public string Authority { get; }

    public ConfigurationManager<OpenIdConnectConfiguration> ConfigurationManager { get; }

    // OK = autorizado, Unauthorized = token ausente/inválido, Forbidden = token válido mas não veio do Entra.
    public async Task<HttpStatusCode> ValidateAsync(HttpRequestData request)
    {
        var header = request.Headers.TryGetValues("Authorization", out var values) ? values.FirstOrDefault() : null;
        if (header is null || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Chamada sem Bearer token");
            return HttpStatusCode.Unauthorized;
        }

        var result = await _handler.ValidateTokenAsync(header["Bearer ".Length..].Trim(), _parameters);
        if (!result.IsValid)
        {
            _logger.LogWarning("Token rejeitado: {Error}", result.Exception?.Message);
            return HttpStatusCode.Unauthorized;
        }

        // Só aceita chamadas do serviço "Azure AD Authentication Events" (azp no token v2, appid no v1).
        var identity = result.ClaimsIdentity;
        if (!identity.HasClaim("azp", _authEventsAppId) && !identity.HasClaim("appid", _authEventsAppId))
        {
            _logger.LogWarning("Token de um cliente não autorizado: azp={Azp} appid={AppId}",
                identity.FindFirst("azp")?.Value, identity.FindFirst("appid")?.Value);
            return HttpStatusCode.Forbidden;
        }

        return HttpStatusCode.OK;
    }
}

// O Entra espera no máximo 2s pela resposta: baixa o metadata/JWKS já no startup e não na 1ª chamada.
public sealed class OidcMetadataWarmup(EntraEventTokenValidator validator, ILogger<OidcMetadataWarmup> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await validator.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);
                logger.LogInformation("Metadata OIDC do External ID carregado ({Authority})", validator.Authority);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao carregar o metadata OIDC ({Authority})", validator.Authority);
            }
        }, CancellationToken.None);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
