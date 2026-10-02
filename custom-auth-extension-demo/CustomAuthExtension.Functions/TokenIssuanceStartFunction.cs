using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using CustomAuthExtension.Api;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CustomAuthExtension.Functions;

public class TokenIssuanceStartFunction(EntraEventTokenValidator validator, ILogger<TokenIssuanceStartFunction> logger)
{
    // AuthorizationLevel.Anonymous: quem autentica a chamada é o Bearer token do Entra, não a function key.
    [Function("TokenIssuanceStart")]
    public async Task<HttpResponseData> TokenIssuanceStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/token-issuance-start")] HttpRequestData req)
    {
        var status = await validator.ValidateAsync(req);
        if (status != HttpStatusCode.OK)
        {
            return req.CreateResponse(status);
        }

        TokenIssuanceStartRequest? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync(req.Body, AppJsonContext.Default.TokenIssuanceStartRequest);
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Payload inválido: {Error}", ex.Message);
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var context = request?.Data?.AuthenticationContext;
        var user = context?.User;

        logger.LogInformation(
            "OnTokenIssuanceStart recebido: correlationId={CorrelationId} app={App} user={UserId} ({UserName}) ip={Ip} payload={Payload}",
            context?.CorrelationId,
            context?.ClientServicePrincipal?.AppDisplayName,
            user?.Id,
            user?.DisplayName,
            context?.Client?.Ip,
            request is null ? null : JsonSerializer.Serialize(request, AppJsonContext.Default.TokenIssuanceStartRequest));

        var claims = DemoClaims.Build(context);

        logger.LogInformation("Claims devolvidas para o token: {Claims}",
            JsonSerializer.Serialize(claims, AppJsonContext.Default.DictionaryStringObject));

        return await JsonResponse(req, new TokenIssuanceStartResponse(
            new TokenIssuanceStartResponseData([new ProvideClaimsForTokenAction(claims)])),
            AppJsonContext.Default.TokenIssuanceStartResponse);
    }

    [Function("Health")]
    public Task<HttpResponseData> Health([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req) =>
        JsonResponse(req, new HealthResponse("healthy", DateTimeOffset.UtcNow), AppJsonContext.Default.HealthResponse);

    private static async Task<HttpResponseData> JsonResponse<T>(HttpRequestData req, T body, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await JsonSerializer.SerializeAsync(response.Body, body, typeInfo);
        return response;
    }
}

public record HealthResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("utc")] DateTimeOffset Utc);

// Serialização via source generator (sem reflection), exigida pelo Native AOT.
// Os tipos concretos que aparecem nos valores das claims (object) precisam estar listados aqui.
[JsonSerializable(typeof(TokenIssuanceStartRequest))]
[JsonSerializable(typeof(TokenIssuanceStartResponse))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(HealthResponse))]
internal partial class AppJsonContext : JsonSerializerContext;
