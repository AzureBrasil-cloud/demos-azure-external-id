namespace saml2.Models;

public sealed class ProtectedViewModel
{
    public IReadOnlyList<ClaimEntry> Claims { get; init; } = [];
}

public sealed record ClaimEntry(string Type, string Value);