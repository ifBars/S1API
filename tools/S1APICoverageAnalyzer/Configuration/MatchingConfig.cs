namespace S1APICoverageAnalyzer.Configuration;

/// <summary>
/// Configuration for type name matching behavior.
/// </summary>
public static class MatchingConfig
{
    /// <summary>
    /// Enable fuzzy matching for type names.
    /// When true, the analyzer will attempt to match types with similar names even if they don't match exactly.
    /// This helps catch cases like:
    /// - Game: "EVehicleColor" vs S1API: "VehicleColor" (enum prefix difference)
    /// - Game: "VehicleColors" vs S1API: "VehicleColor" (plural/singular)
    /// - Game: "ScheduleOne.Vehicles.Modification.EVehicleColor" vs S1API: "S1API.Vehicles.VehicleColor"
    /// </summary>
    public static bool EnableFuzzyMatching { get; set; } = true;
    
    /// <summary>
    /// Minimum similarity score (0.0 to 1.0) required for a fuzzy match to be considered valid.
    /// Higher values = stricter matching, lower values = more lenient matching.
    /// Default: 0.75 (75% similarity required)
    /// </summary>
    public static double FuzzySimilarityThreshold { get; set; } = 0.75;
    
    /// <summary>
    /// When true, logs fuzzy matches to help debug matching behavior.
    /// </summary>
    public static bool VerboseFuzzyMatching { get; set; } = false;
}

