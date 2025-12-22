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
        // Direct match
        if (_wrappedGameTypes.Contains(gameTypeFullName))
            return true;
        
        // Check without nested class prefix (e.g., "ScheduleOne.Casino.SlotMachine+ESymbol" -> "ScheduleOne.Casino.SlotMachine.ESymbol")
        var normalizedName = gameTypeFullName.Replace('+', '.');
        if (_wrappedGameTypes.Contains(normalizedName))
            return true;
        
        // Check if any wrapped type starts with this (for nested types)
        foreach (var wrapped in _wrappedGameTypes)
        {
            if (wrapped.StartsWith(gameTypeFullName + ".", StringComparison.Ordinal) ||
                wrapped.StartsWith(gameTypeFullName + "+", StringComparison.Ordinal))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private string? FindCoveringApiType(string gameTypeFullName)
    {
        foreach (var apiType in _apiTypes)
        {
            if (apiType.WrappedGameTypes.Contains(gameTypeFullName))
                return apiType.FullName;
            
            // Check normalized name
            var normalizedName = gameTypeFullName.Replace('+', '.');
            if (apiType.WrappedGameTypes.Contains(normalizedName))
                return apiType.FullName;
        }
        
        return null;
    }
}
