using System.Text.Json;
using S1APICoverageAnalyzer.Analysis;
using S1APICoverageAnalyzer.Models;
using S1APICoverageAnalyzer.Output;
using Xunit;

namespace S1API.Tests.Coverage;

public sealed class CoverageAnalyzerTests
{
    [Fact]
    public void ApiAnalyzer_RecordsExplicitMappingsAndUnwrapsElementTypes()
    {
        var apiAssembly = typeof(global::S1API.Temperature.TemperatureUtility).Assembly;
        var analyzer = new ApiAssemblyAnalyzer(apiAssembly, apiAssembly.Location);

        analyzer.Analyze();

        IReadOnlyDictionary<string, string> explicitMappings =
            analyzer.GetExplicitCoverageMappings();
        Assert.Equal(
            "S1API.Temperature.TemperatureEmitterInfo",
            explicitMappings["ScheduleOne.Temperature.TemperatureEmitterInfo"]);
        Assert.Equal(
            "S1API.Temperature.TemperatureUtility",
            explicitMappings["ScheduleOne.Temperature.TemperatureUtility"]);

        Assert.Contains(
            "ScheduleOne.Temperature.TemperatureEmitterInfo",
            analyzer.GetWrappedGameTypes());
        Assert.DoesNotContain(
            "ScheduleOne.Temperature.TemperatureEmitterInfo[]",
            analyzer.GetWrappedGameTypes());
    }

    [Fact]
    public void Calculate_ReportsProvenanceAndMatchStrategyForEveryCoveredType()
    {
        var gameTypes = new List<GameType>
        {
            GameType("ScheduleOne.Temperature.TemperatureUtility"),
            GameType("ScheduleOne.Items.ItemDefinition"),
            GameType("ScheduleOne.Casino.BlackjackGameController+EStage"),
            GameType("ScheduleOne.Dialogue.DialogueController.Node"),
            GameType("ScheduleOne.Vehicles.Modification.EVehicleColor")
        };
        var apiTypes = new List<ApiTypeInfo>
        {
            ApiType(
                "S1API.Casino.BlackjackGame",
                "BlackjackGame",
                "ScheduleOne.Casino.BlackjackGameController"),
            ApiType(
                "S1API.Dialogue.DialogueNode",
                "DialogueNode",
                "ScheduleOne.Dialogue.DialogueController+Node"),
            ApiType(
                "S1API.Items.ItemDefinition",
                "ItemDefinition",
                "ScheduleOne.Items.ItemDefinition"),
            ApiType(
                "S1API.Temperature.TemperatureUtility",
                "TemperatureUtility",
                "ScheduleOne.Temperature.TemperatureUtility"),
            ApiType(
                "S1API.Vehicles.VehicleColor",
                "VehicleColor",
                "ScheduleOne.Vehicles.Modification.VehicleColors")
        };
        var explicitMappings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ScheduleOne.Temperature.TemperatureUtility"] =
                "S1API.Temperature.TemperatureUtility"
        };

        CoverageResult result = Calculate(gameTypes, apiTypes, explicitMappings);

        Assert.Collection(
            result.CoveredTypes.OrderBy(type => type.FullName, StringComparer.Ordinal),
            type => AssertMatch(type, "S1API.Casino.BlackjackGame", CoverageMatchStrategy.Nested),
            type => AssertMatch(type, "S1API.Dialogue.DialogueNode", CoverageMatchStrategy.Normalized),
            type => AssertMatch(type, "S1API.Items.ItemDefinition", CoverageMatchStrategy.Exact),
            type => AssertMatch(type, "S1API.Temperature.TemperatureUtility", CoverageMatchStrategy.Explicit),
            type => AssertMatch(type, "S1API.Vehicles.VehicleColor", CoverageMatchStrategy.Fuzzy));
        Assert.Empty(result.UncoveredTypes);

        using JsonDocument report = JsonDocument.Parse(ReportGenerator.GenerateJsonReport(result));
        foreach (JsonElement coveredType in report.RootElement.GetProperty("coveredTypes").EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(coveredType.GetProperty("coveredBy").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(coveredType.GetProperty("matchStrategy").GetString()));
        }
    }

    [Fact]
    public void Calculate_DoesNotFuzzyMatchSimilarUnrelatedType()
    {
        GameType unrelatedGameType =
            GameType("ScheduleOne.Vehicles.VehicleSeatSnapshot");
        ApiTypeInfo similarlyNamedApiType = ApiType(
            "S1API.Items.VehicleSeat",
            "VehicleSeat",
            "ScheduleOne.ItemFramework.ItemSlot");

        CoverageResult result = Calculate(
            new List<GameType> { unrelatedGameType },
            new List<ApiTypeInfo> { similarlyNamedApiType },
            new Dictionary<string, string>());

        Assert.Empty(result.CoveredTypes);
        Assert.Same(unrelatedGameType, Assert.Single(result.UncoveredTypes));
    }

    [Fact]
    public void Calculate_UsesDeterministicApiTypeForEquivalentMatches()
    {
        GameType gameType = GameType("ScheduleOne.Items.ItemDefinition");
        var apiTypes = new List<ApiTypeInfo>
        {
            ApiType(
                "S1API.Zeta.ItemDefinition",
                "ItemDefinition",
                gameType.FullName),
            ApiType(
                "S1API.Alpha.ItemDefinition",
                "ItemDefinition",
                gameType.FullName)
        };

        CoverageResult result = Calculate(
            new List<GameType> { gameType },
            apiTypes,
            new Dictionary<string, string>());

        AssertMatch(
            Assert.Single(result.CoveredTypes),
            "S1API.Alpha.ItemDefinition",
            CoverageMatchStrategy.Exact);
    }

    private static CoverageResult Calculate(
        List<GameType> gameTypes,
        List<ApiTypeInfo> apiTypes,
        IReadOnlyDictionary<string, string> explicitMappings)
    {
        var calculator = new CoverageCalculator(
            gameTypes,
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal),
            apiTypes,
            explicitMappings,
            excludedTypeCount: 0);
        return calculator.Calculate();
    }

    private static GameType GameType(string fullName)
    {
        int separatorIndex = fullName.LastIndexOfAny(['.', '+']);
        return new GameType
        {
            FullName = fullName,
            Namespace = separatorIndex < 0 ? string.Empty : fullName[..separatorIndex],
            Name = separatorIndex < 0 ? fullName : fullName[(separatorIndex + 1)..],
            Kind = GameTypeKind.Class
        };
    }

    private static ApiTypeInfo ApiType(
        string fullName,
        string name,
        params string[] wrappedGameTypes) =>
        new()
        {
            FullName = fullName,
            Name = name,
            WrappedGameTypes = wrappedGameTypes.ToList()
        };

    private static void AssertMatch(
        GameType gameType,
        string expectedApiType,
        CoverageMatchStrategy expectedStrategy)
    {
        Assert.Equal(expectedApiType, gameType.CoveredByApiType);
        Assert.Equal(expectedStrategy, gameType.MatchStrategy);
    }
}
