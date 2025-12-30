namespace S1APICoverageAnalyzer.Models;

/// <summary>
/// Represents a historical entry of coverage analysis.
/// </summary>
public sealed class CoverageHistoryEntry
{
    public required DateTime Timestamp { get; init; }
    
    // Coverage metrics
    public required double ClassCoveragePercentage { get; init; }
    public required double MemberCoveragePercentage { get; init; }
    public required int TotalClasses { get; init; }
    public required int CoveredClasses { get; init; }
    public required int TotalMembers { get; init; }
    public required int CoveredMembers { get; init; }
    public required int ExcludedClasses { get; init; }
    
    // Version tracking
    public required string GameAssemblyVersion { get; init; }
    public required string GameAssemblyHash { get; init; }
    public required string AnalyzerVersion { get; init; }
    
    // Event annotations
    public List<HistoryEvent> Events { get; init; } = new();
    
    // Optional manual note
    public string? Note { get; init; }
}

/// <summary>
/// Represents an event that occurred during coverage analysis.
/// </summary>
public sealed class HistoryEvent
{
    public required EventType Type { get; init; }
    public required string Description { get; init; }
    public string? Details { get; init; }
}

/// <summary>
/// Types of events that can be recorded in coverage history.
/// </summary>
public enum EventType
{
    AnalyzerUpdate,      // Analyzer version changed
    GameUpdate,          // Game assembly changed significantly
    MatchingImproved,    // Fuzzy matching or matching logic improved
    ApiExpansion,        // S1API coverage expanded
    ManualAnnotation     // User-provided note
}

/// <summary>
/// Complete coverage history containing all historical entries.
/// </summary>
public sealed class CoverageHistory
{
    public string GeneratedBy { get; init; } = "S1APICoverageAnalyzer";
    public string Version { get; init; } = "1.0";
    public List<CoverageHistoryEntry> Entries { get; init; } = new();
    
    /// <summary>
    /// Get the most recent entry in the history.
    /// </summary>
    public CoverageHistoryEntry? LatestEntry => 
        Entries.OrderByDescending(e => e.Timestamp).FirstOrDefault();
    
    /// <summary>
    /// Get entries within a date range.
    /// </summary>
    public List<CoverageHistoryEntry> GetEntriesInRange(DateTime start, DateTime end)
    {
        return Entries
            .Where(e => e.Timestamp >= start && e.Timestamp <= end)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }
    
    /// <summary>
    /// Get the last N entries.
    /// </summary>
    public List<CoverageHistoryEntry> GetLastEntries(int count)
    {
        return Entries
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }
}

