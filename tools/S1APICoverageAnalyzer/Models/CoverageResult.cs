namespace S1APICoverageAnalyzer.Models;

/// <summary>
/// Represents the result of coverage analysis.
/// </summary>
public sealed class CoverageResult
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    // Class-level coverage
    public int TotalGameClasses { get; set; }
    public int CoveredGameClasses { get; set; }
    public double ClassCoveragePercentage => 
        TotalGameClasses == 0 ? 0 : (double)CoveredGameClasses / TotalGameClasses * 100;
    
    // Member-level coverage
    public int TotalGameMembers { get; set; }
    public int CoveredGameMembers { get; set; }
    public double MemberCoveragePercentage => 
        TotalGameMembers == 0 ? 0 : (double)CoveredGameMembers / TotalGameMembers * 100;
    
    // Excluded statistics
    public int ExcludedTypeCount { get; set; }
    public List<string> ExcludedNamespaces { get; init; } = new();
    
    // Detailed type information
    public List<GameType> CoveredTypes { get; init; } = new();
    public List<GameType> UncoveredTypes { get; init; } = new();
    
    // API types that provide coverage
    public List<ApiTypeInfo> ApiTypes { get; init; } = new();
}

/// <summary>
/// Information about an S1API type that wraps game types.
/// </summary>
public sealed class ApiTypeInfo
{
    public required string FullName { get; init; }
    public required string Name { get; init; }
    public List<string> WrappedGameTypes { get; init; } = new();
}
