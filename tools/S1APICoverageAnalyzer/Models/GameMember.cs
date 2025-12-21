namespace S1APICoverageAnalyzer.Models;

/// <summary>
/// Represents a member (field, property, or method) of a game type.
/// </summary>
public sealed class GameMember
{
    public required string Name { get; init; }
    public required GameMemberKind Kind { get; init; }
    public required string ReturnType { get; init; }
    public bool IsStatic { get; init; }
    public bool IsCovered { get; set; }
    public string? CoveredByApiType { get; set; }
    public string? CoveredByApiMember { get; set; }
}

public enum GameMemberKind
{
    Field,
    Property,
    Method,
    Event
}
