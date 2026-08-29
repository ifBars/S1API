using S1APICoverageAnalyzer.Configuration;
using S1APICoverageAnalyzer.Models;

namespace S1APICoverageAnalyzer.Analysis;

/// <summary>
/// Calculates coverage by comparing game types against S1API wrapped types.
/// </summary>
public sealed class CoverageCalculator
{
    private readonly List<GameType> _gameTypes;
    private readonly Dictionary<string, HashSet<string>> _accessedMembers;
    private readonly List<ApiTypeInfo> _apiTypes;
    private readonly IReadOnlyDictionary<string, string> _explicitCoverageMappings;
    private readonly int _excludedTypeCount;

    public CoverageCalculator(
        List<GameType> gameTypes,
        Dictionary<string, HashSet<string>> accessedMembers,
        List<ApiTypeInfo> apiTypes,
        IReadOnlyDictionary<string, string> explicitCoverageMappings,
        int excludedTypeCount)
    {
        _gameTypes = gameTypes;
        _accessedMembers = accessedMembers;
        _apiTypes = apiTypes
            .OrderBy(apiType => apiType.FullName, StringComparer.Ordinal)
            .ToList();
        _explicitCoverageMappings = explicitCoverageMappings;
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
            CoverageMatch? match = FindCoverageMatch(gameType.FullName);
            gameType.IsCovered = match != null;
            gameType.CoveredByApiType = match?.ApiTypeName;
            gameType.MatchStrategy = match?.Strategy;

            if (match != null)
            {
                result.CoveredTypes.Add(gameType);

                if (_accessedMembers.TryGetValue(gameType.FullName, out var accessedMemberNames))
                {
                    foreach (var member in gameType.Members)
                    {
                        if (!accessedMemberNames.Contains(member.Name))
                            continue;

                        member.IsCovered = true;
                        member.CoveredByApiType = match.ApiTypeName;
                        coveredMembers++;
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

    private CoverageMatch? FindCoverageMatch(string gameTypeFullName)
    {
        if (_explicitCoverageMappings.TryGetValue(gameTypeFullName, out var explicitApiType) &&
            _apiTypes.Any(apiType => apiType.FullName.Equals(explicitApiType, StringComparison.Ordinal)))
        {
            return new CoverageMatch(explicitApiType, CoverageMatchStrategy.Explicit);
        }

        string? exactApiType = FindApiTypeForWrappedName(gameTypeFullName, normalize: false);
        if (exactApiType != null)
            return new CoverageMatch(exactApiType, CoverageMatchStrategy.Exact);

        string normalizedGameTypeName = NormalizeNestedTypeName(gameTypeFullName);
        string? normalizedApiType = FindApiTypeForWrappedName(normalizedGameTypeName, normalize: true);
        if (normalizedApiType != null)
            return new CoverageMatch(normalizedApiType, CoverageMatchStrategy.Normalized);

        string? nestedApiType = FindNestedMatch(gameTypeFullName);
        if (nestedApiType != null)
            return new CoverageMatch(nestedApiType, CoverageMatchStrategy.Nested);

        string? fuzzyApiType = FindFuzzyMatch(gameTypeFullName);
        return fuzzyApiType == null
            ? null
            : new CoverageMatch(fuzzyApiType, CoverageMatchStrategy.Fuzzy);
    }

    private string? FindApiTypeForWrappedName(string gameTypeName, bool normalize)
    {
        return _apiTypes
            .Where(apiType => apiType.WrappedGameTypes.Any(wrappedType =>
                (normalize ? NormalizeNestedTypeName(wrappedType) : wrappedType)
                    .Equals(gameTypeName, StringComparison.Ordinal)))
            .Select(apiType => apiType.FullName)
            .FirstOrDefault();
    }

    private string? FindNestedMatch(string gameTypeFullName)
    {
        string normalizedGameTypeName = NormalizeNestedTypeName(gameTypeFullName);

        return _apiTypes
            .SelectMany(apiType => apiType.WrappedGameTypes
                .Distinct(StringComparer.Ordinal)
                .Select(wrappedType => new
                {
                    ApiTypeName = apiType.FullName,
                    WrappedTypeName = NormalizeNestedTypeName(wrappedType)
                }))
            .Where(candidate =>
                IsNestedRelation(normalizedGameTypeName, candidate.WrappedTypeName))
            .OrderBy(candidate =>
                Math.Abs(normalizedGameTypeName.Length - candidate.WrappedTypeName.Length))
            .ThenBy(candidate => candidate.ApiTypeName, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.WrappedTypeName, StringComparer.Ordinal)
            .Select(candidate => candidate.ApiTypeName)
            .FirstOrDefault();
    }

    private string? FindFuzzyMatch(string gameTypeFullName)
    {
        if (!MatchingConfig.EnableFuzzyMatching)
            return null;

        double similarityThreshold = MatchingConfig.FuzzySimilarityThreshold;
        double bestScore = 0.0;
        string? bestApiType = null;

        foreach (var apiType in _apiTypes)
        {
            double apiTypeScore = TypeNameMatcher.CalculateSimilarity(
                gameTypeFullName,
                apiType.FullName,
                apiType.Name);
            SelectBetterFuzzyMatch(
                apiTypeScore,
                similarityThreshold,
                apiType.FullName,
                ref bestScore,
                ref bestApiType);

            foreach (string wrappedType in apiType.WrappedGameTypes
                         .Distinct(StringComparer.Ordinal)
                         .OrderBy(typeName => typeName, StringComparer.Ordinal))
            {
                string wrappedSimpleName = wrappedType.Split('.', '+').Last();
                double wrappedTypeScore = TypeNameMatcher.CalculateSimilarity(
                    gameTypeFullName,
                    wrappedType,
                    wrappedSimpleName);
                SelectBetterFuzzyMatch(
                    wrappedTypeScore,
                    similarityThreshold,
                    apiType.FullName,
                    ref bestScore,
                    ref bestApiType);
            }
        }

        if (MatchingConfig.VerboseFuzzyMatching && bestApiType != null)
        {
            Console.WriteLine(
                $"[Fuzzy Match] {gameTypeFullName} -> {bestApiType} (score: {bestScore:F2})");
        }

        return bestApiType;
    }

    private static void SelectBetterFuzzyMatch(
        double score,
        double threshold,
        string apiTypeName,
        ref double bestScore,
        ref string? bestApiType)
    {
        if (score < threshold)
            return;

        bool isBetterScore = score > bestScore;
        bool isDeterministicTieBreak =
            Math.Abs(score - bestScore) < double.Epsilon &&
            (bestApiType == null ||
             StringComparer.Ordinal.Compare(apiTypeName, bestApiType) < 0);

        if (!isBetterScore && !isDeterministicTieBreak)
            return;

        bestScore = score;
        bestApiType = apiTypeName;
    }

    private static bool IsNestedRelation(string left, string right) =>
        !left.Equals(right, StringComparison.Ordinal) &&
        (left.StartsWith(right + ".", StringComparison.Ordinal) ||
         right.StartsWith(left + ".", StringComparison.Ordinal));

    private static string NormalizeNestedTypeName(string typeName) =>
        typeName.Replace('+', '.');

    private sealed record CoverageMatch(
        string ApiTypeName,
        CoverageMatchStrategy Strategy);
}
