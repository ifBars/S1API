using System.Reflection;
using Newtonsoft.Json;
using S1API.Internal.Map;
using S1API.Map;
using UnityEngine;

#if IL2CPPMELON
using S1Map = Il2CppScheduleOne.Map;
using S1Cartel = Il2CppScheduleOne.Cartel;
using S1Levelling = Il2CppScheduleOne.Levelling;
using S1ContactsApp = Il2CppScheduleOne.UI.Phone.ContactsApp;
#elif MONOMELON
using S1Map = ScheduleOne.Map;
using S1Cartel = ScheduleOne.Cartel;
using S1Levelling = ScheduleOne.Levelling;
using S1ContactsApp = ScheduleOne.UI.Phone.ContactsApp;
#endif

namespace S1API.Tests.Map;

[Collection("CustomRegions")]
public sealed class CustomRegionTests : IDisposable
{
    // Vector2 is built field by field: its constructor needs the native runtime in the Il2Cpp configuration.
    private static Vector2 P(float x, float y)
    {
        var v = default(Vector2);
        v.x = x;
        v.y = y;
        return v;
    }

    private static readonly Vector2[] Square =
    {
        P(0, 0), P(10, 0), P(10, 10), P(0, 10),
    };

    private readonly List<(string Level, string Message)> _log = new();

    public CustomRegionTests()
    {
        CustomRegionRegistry.ResetForTesting();
        CustomRegionRegistry.SkipLifecycleHookForTesting = true;
        CustomRegionRegistry.LogSinkForTesting = (level, message) => _log.Add((level, message));
    }

    public void Dispose()
    {
        CustomRegionRegistry.ResetForTesting();
        CustomRegionRegistry.SkipLifecycleHookForTesting = false;
        CustomRegionRegistry.LogSinkForTesting = null;
    }

    private static RegionDefinition Def(string id, string name = "Harbor") => new RegionDefinition
    {
        Id = id,
        Name = name,
        Polygon = Square,
        StartInfluence = 0.4f,
    };

    private static int HighestVanilla() =>
        Enum.GetValues(typeof(S1Map.EMapRegion)).Cast<object>().Max(v => Convert.ToInt32(v));

    [Fact]
    public void CustomNumbersSitAboveTheHighestVanillaRegion()
    {
        Assert.Equal(HighestVanilla() + 1, CustomRegionRegistry.FirstValue);
        var a = Regions.Register(Def("b.region"));
        var b = Regions.Register(Def("a.region"));
        CustomRegionRegistry.Freeze();

        Assert.True(a.IsAssigned);
        Assert.True((int)a.Value > HighestVanilla());
        Assert.True((int)b.Value > HighestVanilla());
        Assert.NotEqual(a.Value, b.Value);
    }

    [Fact]
    public void NumbersFollowTheSortedIdsNotTheRegistrationOrder()
    {
        var late = Regions.Register(Def("z.region"));
        var early = Regions.Register(Def("a.region"));
        CustomRegionRegistry.Freeze();

        Assert.Equal(CustomRegionRegistry.FirstValue, (int)early.Value);
        Assert.Equal(CustomRegionRegistry.FirstValue + 1, (int)late.Value);
    }

    [Fact]
    public void ValueThrowsBeforeTheNumbersAreFixed()
    {
        var handle = Regions.Register(Def("x.region"));

        Assert.False(handle.IsAssigned);
        Assert.Throws<InvalidOperationException>(() => handle.Value);
    }

    [Fact]
    public void CustomListKeepsRegistrationOrderAndGetFindsById()
    {
        var first = Regions.Register(Def("z.region"));
        var second = Regions.Register(Def("a.region"));

        Assert.Equal(new[] { first, second }, Regions.Custom.ToArray());
        Assert.Same(second, Regions.Get("A.REGION"));
        Assert.Null(Regions.Get("missing"));
    }

    [Fact]
    public void SameDefinitionTwiceReturnsTheSameHandle()
    {
        var def = Def("x.region");
        Assert.Same(Regions.Register(def), Regions.Register(def));
        Assert.Single(Regions.Custom);
    }

    [Fact]
    public void RegisteringAnIdAgainReplacesTheDefinition()
    {
        var first = Regions.Register(Def("x.region", "Old"));
        var second = Regions.Register(Def("X.REGION", "New"));

        Assert.Same(first, second);
        Assert.Equal("New", first.Name);
        Assert.Single(Regions.Custom);
    }

    [Fact]
    public void ChangingTheDefinitionAfterRegisteringHasNoEffect()
    {
        var def = Def("x.region", "Before");
        var handle = Regions.Register(def);
        def.Name = "After";

        Assert.Equal("Before", handle.Name);
    }

    [Fact]
    public void InvalidDefinitionsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => Regions.Register(null!));
        Assert.Throws<ArgumentException>(() => Regions.Register(Def(" ")));
        Assert.Throws<ArgumentException>(() => Regions.Register(Def("x", "")));
        var fewPoints = Def("x");
        fewPoints.Polygon = new[] { P(0, 0), P(1, 1) };
        Assert.Throws<ArgumentException>(() => Regions.Register(fewPoints));
        var nan = Def("x");
        nan.Polygon = new[] { P(0, 0), P(1, float.NaN), P(2, 2) };
        Assert.Throws<ArgumentException>(() => Regions.Register(nan));
        var influence = Def("x");
        influence.StartInfluence = 1.5f;
        Assert.Throws<ArgumentException>(() => Regions.Register(influence));
        var standIn = Def("x");
        standIn.StandIn = (Region)99;
        Assert.Throws<ArgumentException>(() => Regions.Register(standIn));
        Assert.Empty(Regions.Custom);
    }

    [Fact]
    public void UnregisterRemovesARegionThatHasNoWorldYet()
    {
        var a = Regions.Register(Def("a.region"));
        var b = Regions.Register(Def("b.region"));

        Assert.True(Regions.Unregister(a));
        Assert.False(Regions.Unregister(a));
        Assert.Equal(new[] { b }, Regions.Custom.ToArray());
        Assert.Null(Regions.Get("a.region"));
        Assert.Throws<ArgumentNullException>(() => Regions.Unregister(null!));
        CustomRegionRegistry.Freeze();
        Assert.Equal(CustomRegionRegistry.FirstValue, (int)b.Value);
    }

    [Fact]
    public void RegisteringAnUnregisteredIdAgainGivesANewHandle()
    {
        var first = Regions.Register(Def("a.region"));
        Regions.Unregister(first);

        var second = Regions.Register(Def("a.region"));

        Assert.NotSame(first, second);
        Assert.Single(Regions.Custom);
    }

    [Fact]
    public void RegistrationAfterTheFirstWorldGetsTheNextNumberAndWarns()
    {
        var a = Regions.Register(Def("a.region"));
        CustomRegionRegistry.Freeze();
        var late = Regions.Register(Def("0.region"));

        Assert.Equal((int)a.Value + 1, (int)late.Value);
        Assert.Contains(_log, l => l.Level == "warn" && l.Message.Contains("0.region"));
    }

    [Fact]
    public void OnlyRegisteredNumbersAreCustom()
    {
        var handle = Regions.Register(Def("x.region"));
        Assert.False(CustomRegionRegistry.IsCustom(S1Map.EMapRegion.Uptown));
        Assert.False(CustomRegionRegistry.IsCustom((S1Map.EMapRegion)CustomRegionRegistry.FirstValue));

        CustomRegionRegistry.Freeze();

        Assert.True(CustomRegionRegistry.IsCustom((S1Map.EMapRegion)(int)handle.Value));
        Assert.False(CustomRegionRegistry.IsCustom(S1Map.EMapRegion.Uptown));
        Assert.False(CustomRegionRegistry.IsCustom((S1Map.EMapRegion)((int)handle.Value + 1)));
    }

    [Fact]
    public void HandleOperationsWithoutAWorldWarnInsteadOfThrowing()
    {
        var handle = Regions.Register(Def("x.region"));

        Assert.False(handle.IsUnlocked);
        Assert.Equal(0.4f, handle.Influence);
        handle.Unlock();
        handle.ChangeInfluence(0.1f);

        Assert.Equal(2, _log.Count(l => l.Level == "warn" && l.Message.Contains("x.region")));
    }

    [Fact]
    public void SavedStateRoundTripsAndKeepsRegionsOfOtherMods()
    {
        string folder = Path.Combine(Path.GetTempPath(), "s1api-regions-" + Guid.NewGuid().ToString("N"));
        try
        {
            var warnings = new List<string>();
            Assert.Equal(2, CustomRegionState.Write(folder, new[]
            {
                new CustomRegionSavedState { Id = "a", Unlocked = true, Influence = 0.25f },
                new CustomRegionSavedState { Id = "b", Unlocked = false, Influence = 0.9f },
            }, warnings.Add));
            Assert.Equal(2, CustomRegionState.Write(folder, new[]
            {
                new CustomRegionSavedState { Id = "A", Unlocked = false, Influence = 0.5f },
            }, warnings.Add));

            var read = CustomRegionState.Read(folder, warnings.Add);

            Assert.Equal(2, read.Count);
            Assert.False(read["a"].Unlocked);
            Assert.Equal(0.5f, read["a"].Influence);
            Assert.Equal(0.9f, read["b"].Influence);
            Assert.Empty(warnings);
            Assert.False(File.Exists(CustomRegionState.PathIn(folder) + ".tmp"));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void SavingNothingLeavesTheFolderUntouched()
    {
        string folder = Path.Combine(Path.GetTempPath(), "s1api-regions-" + Guid.NewGuid().ToString("N"));

        Assert.Equal(0, CustomRegionState.Write(folder, Array.Empty<CustomRegionSavedState>(), _ => { }));
        Assert.False(Directory.Exists(folder));
    }

    [Fact]
    public void MissingFileIsANewSaveWithoutAWarning()
    {
        var warnings = new List<string>();

        Assert.Empty(CustomRegionState.Read(Path.Combine(Path.GetTempPath(), "s1api-none-" + Guid.NewGuid().ToString("N")), warnings.Add));
        Assert.Empty(CustomRegionState.Read(null, warnings.Add));
        Assert.Empty(warnings);
    }

    [Fact]
    public void UnreadableFileWarnsAndStartsFromDefaults()
    {
        string folder = Path.Combine(Path.GetTempPath(), "s1api-regions-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CustomRegionState.PathIn(folder))!);
            File.WriteAllText(CustomRegionState.PathIn(folder), "{ not json");
            var warnings = new List<string>();

            Assert.Empty(CustomRegionState.Read(folder, warnings.Add));
            Assert.Single(warnings);

            File.WriteAllText(CustomRegionState.PathIn(folder), JsonConvert.SerializeObject(new CustomRegionSaveFile { FormatVersion = 99 }));
            warnings.Clear();

            Assert.Empty(CustomRegionState.Read(folder, warnings.Add));
            Assert.Single(warnings);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void SavedInfluenceIsClamped()
    {
        string folder = Path.Combine(Path.GetTempPath(), "s1api-regions-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CustomRegionState.PathIn(folder))!);
            File.WriteAllText(CustomRegionState.PathIn(folder),
                "{\"FormatVersion\":1,\"Regions\":[{\"Id\":\"a\",\"Unlocked\":true,\"Influence\":7.0}]}");

            Assert.Equal(1f, CustomRegionState.Read(folder, _ => { })["a"].Influence);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    private const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    [Theory]
    [InlineData(typeof(S1Map.Map), "Awake")]
    [InlineData(typeof(S1Map.Map), "OnRankUp")]
    [InlineData(typeof(S1Cartel.CartelInfluence), "Awake")]
    [InlineData(typeof(S1Cartel.CartelInfluence), "RpcLogic___ChangeInfluence_1267088319")]
    [InlineData(typeof(S1Cartel.CartelInfluence), "GetAllRegionInfluence")]
    [InlineData(typeof(S1Cartel.CartelActivities), "GetInfluenceFraction")]
    [InlineData(typeof(S1Cartel.CartelActivities), "GetRegionalActivities")]
    [InlineData(typeof(S1ContactsApp.ContactsApp), "Start")]
    [InlineData(typeof(S1ContactsApp.ContactsApp), "SetSelectedRegion")]
    [InlineData(typeof(S1Levelling.LevelManager), "GetSaveString")]
    [InlineData(typeof(S1Levelling.LevelManager), "SetData")]
    public void PatchTargetsExistInTargetRuntime(Type type, string method)
    {
        Assert.NotNull(type.GetMethods(Declared).FirstOrDefault(m => m.Name == method));
    }
}
