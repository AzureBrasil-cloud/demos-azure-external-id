namespace CustomAuthExtension.Web.Models;

public record ClaimEntry(string Type, string Value, bool FromExtension);

public class TokenViewModel
{
    public string? IdToken { get; init; }
    public string? HeaderJson { get; init; }
    public string? PayloadJson { get; init; }
    public DateTimeOffset? IssuedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? Issuer { get; init; }
    public string? Audience { get; init; }
    public string? CookieExpiresAt { get; init; }
    public IReadOnlyList<ClaimEntry> Claims { get; init; } = [];
    public IReadOnlyList<ClaimEntry> ExtensionClaims => Claims.Where(c => c.FromExtension).ToList();
}

public static class ExtensionClaimTypes
{
    // Nomes das claims no token (JwtClaimType da claimsMappingPolicy com Source=CustomClaimsProvider).
    public static readonly string[] All =
    [
        "loyalty_tier",
        "customer_segment",
        "correlation_id",
        "extension_called_at",
        "demo_roles"
    ];

    public static bool Contains(string claimType) => All.Contains(claimType, StringComparer.OrdinalIgnoreCase);
}
