using S1APICoverageAnalyzer.Configuration;
using S1APICoverageAnalyzer.Models;

namespace S1APICoverageAnalyzer.Analysis;

/// <summary>
/// Calculates coverage by comparing game types against S1API wrapped types.
/// </summary>
public sealed class CoverageCalculator
{
    private readonly List<GameType> _gameTypes;
    private readonly HashSet<string> _wrappedGameTypes;
    private readonly Dictionary<string, HashSet<string>> _accessedMembers;
    private readonly List<ApiTypeInfo> _apiTypes;
    private readonly int _excludedTypeCount;
    private readonly Dictionary<string, string> _fuzzyMatchCache = new();
    
    public CoverageCalculator(
        List<GameType> gameTypes,
        HashSet<string> wrappedGameTypes,
        Dictionary<string, HashSet<string>> accessedMembers,
        List<ApiTypeInfo> apiTypes,
        int excludedTypeCount)
    {
        _gameTypes = gameTypes;
        _wrappedGameTypes = wrappedGameTypes;
        _accessedMembers = accessedMembers;
        _apiTypes = apiTypes;
        _excludedTypeCount = excludedTypeCount;
    }
    
    /// <summary>
    /// Calculate coverage and return the result.
    /// </summary>
    public CoverageResult Calculate()
    {
        var result = new CoverageResult
        {
            ExcludedTypeCount = _excludedTypeCount,
            ExcludedNamespaces = ExclusionConfig.ExcludedNamespaces
                .Concat(ExclusionConfig.InfrastructureNamespaces)
                .ToList(),
            ApiTypes = _apiTypes
        };
        
        int totalMembers = 0;
        int coveredMembers = 0;
        
        foreach (var gameType in _gameTypes)
        {
            // Check if this game type is covered by S1API
            bool isCovered = IsTypeCovered(gameType.FullName);
            gameType.IsCovered = isCovered;
            
            if (isCovered)
            {
                // Find which API type covers this game type
                gameType.CoveredByApiType = FindCoveringApiType(gameType.FullName);
                result.CoveredTypes.Add(gameType);
                
                // Check member coverage
                if (_accessedMembers.TryGetValue(gameType.FullName, out var accessedMemberNames))
                {
                    foreach (var member in gameType.Members)
                    {
                        if (accessedMemberNames.Contains(member.Name))
                        {
                            member.IsCovered = true;
                            member.CoveredByApiType = gameType.CoveredByApiType;
                            coveredMembers++;
                        }
                    }
                }
            }
            else
            {
                result.UncoveredTypes.Add(gameType);
            }
            
            totalMembers += gameType.Members.Count;
        }
        
        result.TotalGameClasses = _gameTypes.Count;
        result.CoveredGameClasses = result.CoveredTypes.Count;
        result.TotalGameMembers = totalMembers;
        result.CoveredGameMembers = coveredMembers;
        
        return result;
    }
    
    private bool IsTypeCovered(string gameTypeFullName)
    {
        // Strategy 1: Direct exact match
        if (_wrappedGameTypes.Contains(gameTypeFullName))
            return true;
        
        // Strategy 2: Normalized nested class separator (+ to . and vice versa)
        var normalizedName = gameTypeFullName.Replace('+', '.');
        if (_wrappedGameTypes.Contains(normalizedName))
            return true;
        
        // Also try converting wrapped types from . to +
        foreach (var wrapped in _wrappedGameTypes)
        {
            var wrappedNormalized = wrapped.Replace('+', '.');
            if (wrappedNormalized == normalizedName || wrappedNormalized == gameTypeFullName)
                return true;
        }
        
        // Strategy 3: Check if any wrapped type starts with this (for nested types)
        // Handle both + and . separators
        foreach (var wrapped in _wrappedGameTypes)
        {
            // Check if wrapped type is a parent of this nested type
            if (wrapped.StartsWith(gameTypeFullName + ".", StringComparison.Ordinal) ||
                wrapped.StartsWith(gameTypeFullName + "+", StringComparison.Ordinal))
            {
                return true;
            }
            
            // Check if this type is a nested type within wrapped
            // e.g., gameTypeFullName = "ScheduleOne.Console+PackageProduct"
            // wrapped = "ScheduleOne.Console"
            if (gameTypeFullName.StartsWith(wrapped + "+", StringComparison.Ordinal) ||
                gameTypeFullName.StartsWith(wrapped + ".", StringComparison.Ordinal))
            {
                return true;
            }
            
            // Also check normalized versions
            var wrappedNormalized = wrapped.Replace('+', '.');
            var gameNormalized = gameTypeFullName.Replace('+', '.');
            if (wrappedNormalized.StartsWith(gameNormalized + ".", StringComparison.Ordinal) ||
                gameNormalized.StartsWith(wrappedNormalized + ".", StringComparison.Ordinal))
            {
                return true;
            }
        }
        
        // Strategy 4: Fuzzy matching based on type names
        // This handles cases like:
        // - Game: "ScheduleOne.Vehicles.Modification.EVehicleColor" vs S1API: "S1API.Vehicles.VehicleColor"
        // - Game: "ScheduleOne.Vehicles.Modification.VehicleColors" vs S1API: "S1API.Vehicles.VehicleColor"
        // - Nested types: "ScheduleOne.Console+PackageProduct" vs wrapper that uses Console
        var fuzzyMatch = FindFuzzyMatch(gameTypeFullName);
        if (fuzzyMatch != null)
        {
            _fuzzyMatchCache[gameTypeFullName] = fuzzyMatch;
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Find a fuzzy match for a game type among API types.
    /// Uses similarity scoring to find the best match above a threshold.
    /// </summary>
    private string? FindFuzzyMatch(string gameTypeFullName)
    {
        // Check if fuzzy matching is enabled
        if (!Configuration.MatchingConfig.EnableFuzzyMatching)
            return null;
        
        double similarityThreshold = Configuration.MatchingConfig.FuzzySimilarityThreshold;
        double bestScore = 0.0;
        string? bestMatch = null;
        
        foreach (var apiType in _apiTypes)
        {
            // Calculate similarity between game type and API type
            var score = TypeNameMatcher.CalculateSimilarity(
                gameTypeFullName,
                apiType.FullName,
                apiType.Name);
            
            if (score > bestScore && score >= similarityThreshold)
            {
                bestScore = score;
                bestMatch = apiType.FullName;
            }
        }
        
        // Also check against wrapped game types directly
        // (in case the API wraps a game type with a different name)
        foreach (var wrappedType in _wrappedGameTypes)
        {
            var wrappedSimpleName = wrappedType.Split('.', '+').Last();
            var score = TypeNameMatcher.CalculateSimilarity(
                gameTypeFullName,
                wrappedType,
                wrappedSimpleName);
            
            if (score > bestScore && score >= similarityThreshold)
            {
                bestScore = score;
                bestMatch = wrappedType;
            }
        }
        
        if (Configuration.MatchingConfig.VerboseFuzzyMatching && bestMatch != null)
        {
            Console.WriteLine($"[Fuzzy Match] {gameTypeFullName} -> {bestMatch} (score: {bestScore:F2})");
        }
        
        return bestMatch;
    }
    
    private string? FindCoveringApiType(string gameTypeFullName)
    {
        // First try exact matches
        foreach (var apiType in _apiTypes)
        {
            if (apiType.WrappedGameTypes.Contains(gameTypeFullName))
                return apiType.FullName;
            
            // Check normalized name
            var normalizedName = gameTypeFullName.Replace('+', '.');
            if (apiType.WrappedGameTypes.Contains(normalizedName))
                return apiType.FullName;
        }
        
        // If we found a fuzzy match earlier, return it
        if (_fuzzyMatchCache.TryGetValue(gameTypeFullName, out var cachedMatch))
            return cachedMatch;
        
        return null;
    }
}
