namespace S1APICoverageAnalyzer.Configuration;

/// <summary>
/// Declares semantic coverage that cannot be inferred from native type references.
/// </summary>
internal static class ExplicitCoverageConfig
{
    private static readonly IReadOnlyDictionary<string, string> Mappings =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ScheduleOne.Temperature.TemperatureEmitterInfo"] =
                "S1API.Temperature.TemperatureEmitterInfo",
            ["ScheduleOne.Temperature.TemperatureUtility"] =
                "S1API.Temperature.TemperatureUtility"
        };

    public static IReadOnlyDictionary<string, string> GetMappings() =>
        Mappings;

    public static IEnumerable<string> GetGameTypesCoveredBy(string apiTypeName) =>
        Mappings
            .Where(mapping => mapping.Value.Equals(apiTypeName, StringComparison.Ordinal))
            .Select(mapping => mapping.Key)
            .OrderBy(gameTypeName => gameTypeName, StringComparer.Ordinal);
}
