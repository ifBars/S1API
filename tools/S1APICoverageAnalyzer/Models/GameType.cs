namespace S1APICoverageAnalyzer.Models;

/// <summary>
/// Represents a game type from the ScheduleOne namespace.
/// </summary>
public sealed class GameType
{
    public required string FullName { get; init; }
    public required string Namespace { get; init; }
    public required string Name { get; init; }
    public required GameTypeKind Kind { get; init; }
    public List<GameMember> Members { get; init; } = new();
    
    /// <summary>
    /// Whether this type has any coverage by S1API.
    /// </summary>
    public bool IsCovered { get; set; }
    
    /// <summary>
    /// The S1API wrapper type that provides coverage for this game type.
    /// </summary>
    public string? CoveredByApiType { get; set; }
    
    /// <summary>
    /// Number of members that are covered.
    /// </summary>
    public int CoveredMemberCount => Members.Count(m => m.IsCovered);
    
    /// <summary>
    /// Total number of members.
    /// </summary>
    public int TotalMemberCount => Members.Count;
    
    /// <summary>
    /// Member coverage percentage for this type.
    /// </summary>
    public double MemberCoveragePercentage => 
        TotalMemberCount == 0 ? 0 : (double)CoveredMemberCount / TotalMemberCount * 100;
}

public enum GameTypeKind
{
    Class,
    Struct,
    Interface,
    Enum,
    Delegate
}
