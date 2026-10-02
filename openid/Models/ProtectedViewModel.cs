namespace teste_b2c.Models;

public sealed class ProtectedViewModel
{
    public IReadOnlyList<ClaimEntry> Claims { get; init; } = [];

    public string? IdToken { get; init; }
}

public sealed record ClaimEntry(string Type, string Value);