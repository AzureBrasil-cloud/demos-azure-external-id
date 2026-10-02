namespace CustomAuthExtension.Api;

public static class DemoClaims
{
    // Regras fictícias para a demo: num cenário real aqui entraria CRM, ERP, banco etc.
    public static Dictionary<string, object> Build(AuthenticationContext? context)
    {
        var user = context?.User;
        var email = user?.Mail ?? user?.UserPrincipalName ?? string.Empty;
        var domain = email.Contains('@') ? email[(email.IndexOf('@') + 1)..].ToLowerInvariant() : string.Empty;

        var tier = domain switch
        {
            "azurebrasil.cloud" => "Gold",
            "microsoft.com" => "Platinum",
            _ => (user?.Id?.Length ?? 0) % 2 == 0 ? "Silver" : "Bronze"
        };

        var roles = new List<string> { "demo.reader" };
        if (tier is "Gold" or "Platinum")
        {
            roles.Add("demo.admin");
        }

        return new Dictionary<string, object>
        {
            ["loyaltyTier"] = tier,
            ["customerSegment"] = string.IsNullOrEmpty(domain) ? "consumer" : $"domain:{domain}",
            ["correlationId"] = context?.CorrelationId ?? Guid.NewGuid().ToString(),
            ["extensionCalledAt"] = DateTimeOffset.UtcNow.ToString("O"),
            ["demoRoles"] = roles.ToArray()
        };
    }
}
