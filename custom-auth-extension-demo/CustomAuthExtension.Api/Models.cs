using System.Text.Json.Serialization;

namespace CustomAuthExtension.Api;

// Payload enviado pelo Entra External ID no evento OnTokenIssuanceStart.
public record TokenIssuanceStartRequest(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("data")] TokenIssuanceStartData? Data);

public record TokenIssuanceStartData(
    [property: JsonPropertyName("tenantId")] string? TenantId,
    [property: JsonPropertyName("authenticationEventListenerId")] string? AuthenticationEventListenerId,
    [property: JsonPropertyName("customAuthenticationExtensionId")] string? CustomAuthenticationExtensionId,
    [property: JsonPropertyName("authenticationContext")] AuthenticationContext? AuthenticationContext);

public record AuthenticationContext(
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("protocol")] string? Protocol,
    [property: JsonPropertyName("client")] ClientContext? Client,
    [property: JsonPropertyName("clientServicePrincipal")] ServicePrincipalContext? ClientServicePrincipal,
    [property: JsonPropertyName("user")] UserContext? User);

public record ClientContext(
    [property: JsonPropertyName("ip")] string? Ip,
    [property: JsonPropertyName("locale")] string? Locale,
    [property: JsonPropertyName("market")] string? Market);

public record ServicePrincipalContext(
    [property: JsonPropertyName("appId")] string? AppId,
    [property: JsonPropertyName("appDisplayName")] string? AppDisplayName);

public record UserContext(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("givenName")] string? GivenName,
    [property: JsonPropertyName("surname")] string? Surname,
    [property: JsonPropertyName("mail")] string? Mail,
    [property: JsonPropertyName("userPrincipalName")] string? UserPrincipalName,
    [property: JsonPropertyName("userType")] string? UserType,
    [property: JsonPropertyName("createdDateTime")] string? CreatedDateTime);

// Resposta esperada pelo Entra: uma action provideClaimsForToken com as claims extras.
public record TokenIssuanceStartResponse(
    [property: JsonPropertyName("data")] TokenIssuanceStartResponseData Data);

public record TokenIssuanceStartResponseData(
    [property: JsonPropertyName("actions")] IReadOnlyList<ProvideClaimsForTokenAction> Actions)
{
    [JsonPropertyName("@odata.type")]
    [JsonPropertyOrder(-1)]
    public string ODataType => "microsoft.graph.onTokenIssuanceStartResponseData";
}

public record ProvideClaimsForTokenAction(
    [property: JsonPropertyName("claims")] IReadOnlyDictionary<string, object> Claims)
{
    [JsonPropertyName("@odata.type")]
    [JsonPropertyOrder(-1)]
    public string ODataType => "microsoft.graph.tokenIssuanceStart.provideClaimsForToken";
}
