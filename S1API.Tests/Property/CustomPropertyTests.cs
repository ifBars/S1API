using System.Reflection;
using HarmonyLib;
using S1API.Internal.Patches;
using S1API.Internal.Properties;
using S1API.Property;

#if IL2CPPMELON
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
using S1Heatmap = Il2CppScheduleOne.Heatmap;
using S1Loaders = Il2CppScheduleOne.Persistence.Loaders;
using S1Property = Il2CppScheduleOne.Property;
#elif MONOMELON
using S1Datas = ScheduleOne.Persistence.Datas;
using S1Heatmap = ScheduleOne.Heatmap;
using S1Loaders = ScheduleOne.Persistence.Loaders;
using S1Property = ScheduleOne.Property;
#endif

namespace S1API.Tests.Property;

[Collection("PropertyHooks")]
public sealed class CustomPropertyTests : IDisposable
{
    private readonly List<(string Level, string Message)> _log = new();
    private readonly HashSet<string> _inWorld = new();
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "s1api-customprop-" + Guid.NewGuid().ToString("N"));
    private float _now;

    public CustomPropertyTests()
    {
        CustomProperties.ResetForTesting();
        CustomProperties.LogSinkForTesting = (level, message) => _log.Add((level, message));
        CustomProperties.PropertyExistsForTesting = code => _inWorld.Contains(code);
        CustomProperties.NowForTesting = () => _now;
        Directory.CreateDirectory(Path.Combine(_dir, "Properties"));
    }

    public void Dispose()
    {
        CustomProperties.ResetForTesting();
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    private static CustomPropertyDefinition Def(string code, Func<UnityEngine.Transform?>? root = null, bool keep = true) =>
        new() { Code = code, Name = code, Root = root, KeepSaveWhenMissing = keep };

    private string WriteSave(string file, string code)
    {
        File.WriteAllText(Path.Combine(_dir, "Properties", file), "{\"DataType\":\"PropertyData\",\"PropertyCode\":\"" + code + "\"}");
        return file;
    }

    private List<string> Kept()
    {
        var kept = new List<string>();
        CustomProperties.KeepSavedFiles("property", "Properties", _dir, kept.Add);
        return kept;
    }

    // ---- registration ----

    [Fact]
    public void RegisterReturnsWrapperWithCodeBeforeThePropertyExists()
    {
        var wrapper = PropertyManager.Register(Def("annex"));

        Assert.Equal("annex", wrapper.PropertyCode);
        Assert.False(wrapper.IsInWorld);
        Assert.True(CustomProperties.IsRegistered("annex"));
        Assert.Contains(_log, l => l.Level == "info" && l.Message.Contains("Registered custom property 'annex'"));
    }

    [Fact]
    public void ContentCullingLockWorksOnAWrapperWithoutProperty()
    {
        PropertyHooks.ResetForTesting();
        PropertyHooks.LogSinkForTesting = (_, _) => { };
        var wrapper = PropertyManager.Register(Def("annex"));

        wrapper.ContentCullingLocked = true;

        Assert.True(PropertyHooks.IsCullingLocked("annex"));
        PropertyHooks.ResetForTesting();
    }

    [Fact]
    public void RegisterRefusesNullAndEmptyCode()
    {
        Assert.Throws<ArgumentNullException>(() => PropertyManager.Register(null!));
        Assert.Throws<ArgumentException>(() => PropertyManager.Register(Def("")));

        Assert.Equal(0, CustomProperties.RegisteredCount);
        Assert.Equal(1, _log.Count(l => l.Level == "error"));
    }

    [Fact]
    public void RegistrationKeepsACopyOfTheDefinition()
    {
        var def = Def("annex");
        PropertyManager.Register(def);
        def.KeepSaveWhenMissing = true;
        def.Code = "other";

        Assert.True(CustomProperties.IsRegistered("annex"));
        Assert.False(CustomProperties.IsRegistered("other"));
    }

    // ---- save keeping ----

    [Fact]
    public void SaveOfMissingRegisteredPropertyIsKept()
    {
        PropertyManager.Register(Def("annex"));
        var file = WriteSave("Annex.json", "annex");
        WriteSave("Vanilla.json", "manor");

        Assert.Equal(new[] { file }, Kept());
        Assert.Contains(_log, l => l.Level == "info" && l.Message.Contains("'annex' is not in the world"));
    }

    [Fact]
    public void SaveOfPropertyInTheWorldIsLeftToVanilla()
    {
        PropertyManager.Register(Def("annex"));
        WriteSave("Annex.json", "annex");
        _inWorld.Add("annex");

        Assert.Empty(Kept());
    }

    [Fact]
    public void SaveIsNotKeptWhenTheDefinitionOptsOut()
    {
        PropertyManager.Register(Def("annex", keep: false));
        WriteSave("Annex.json", "annex");

        Assert.Empty(Kept());
    }

    [Fact]
    public void UnreadableSaveFileIsKeptWithWarning()
    {
        PropertyManager.Register(Def("annex"));
        var path = Path.Combine(_dir, "Properties", "Annex.json");
        WriteSave("Annex.json", "annex");
        using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);

        var kept = Kept();

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(new[] { "Annex.json" }, kept);
            Assert.Contains(_log, l => l.Level == "warn" && l.Message.Contains("is kept"));
        }
    }

    [Fact]
    public void MissingSaveFolderKeepsNothing()
    {
        PropertyManager.Register(Def("annex"));
        var kept = new List<string>();

        CustomProperties.KeepSavedFiles("business", "Businesses", _dir, kept.Add);

        Assert.Empty(kept);
        Assert.DoesNotContain(_log, l => l.Level == "error");
    }

    // ---- deferred loads ----

    [Fact]
    public void LoadOfUnregisteredPropertyIsNotDeferred()
    {
        Assert.False(CustomProperties.TryDeferLoad("manor", "test", () => { }));
        Assert.False(CustomProperties.TryDeferLoad(null, "test", () => { }));
    }

    [Fact]
    public void LoadOfReadyPropertyIsNotDeferred()
    {
        PropertyManager.Register(Def("annex"));
        _inWorld.Add("annex");

        Assert.False(CustomProperties.TryDeferLoad("annex", "test", () => { }));
    }

    [Fact]
    public void LoadWaitsForThePropertyThenRunsOnce()
    {
        PropertyManager.Register(Def("annex"));
        int runs = 0;

        Assert.True(CustomProperties.TryDeferLoad("annex", "test", () => runs++));
        Assert.True(CustomProperties.Pump());
        Assert.Equal(0, runs);

        _inWorld.Add("annex");
        Assert.False(CustomProperties.Pump());
        Assert.Equal(1, runs);
        Assert.False(CustomProperties.Pump());
        Assert.Equal(1, runs);
        Assert.Contains(_log, l => l.Message.Contains("Loading 'annex' runs now"));
    }

    [Fact]
    public void LoadWaitsForTheRootToo()
    {
        UnityEngine.Transform? root = null;
        PropertyManager.Register(Def("annex", () => root));
        _inWorld.Add("annex");
        int runs = 0;

        Assert.True(CustomProperties.TryDeferLoad("annex", "test", () => runs++));
        CustomProperties.Pump();
        Assert.Equal(0, runs);
    }

    [Fact]
    public void RootThatThrowsCountsAsNotReadyAndIsLoggedOnce()
    {
        PropertyManager.Register(Def("annex", () => throw new InvalidOperationException("no world")));
        _inWorld.Add("annex");

        Assert.True(CustomProperties.TryDeferLoad("annex", "test", () => { }));
        CustomProperties.Pump();
        CustomProperties.Pump();

        Assert.Single(_log, l => l.Level == "error" && l.Message.Contains("Root of custom property 'annex' threw"));
    }

    [Fact]
    public void LoadGivesUpAfterTheDeadlineAndSaysSo()
    {
        PropertyManager.Register(Def("annex"));
        int runs = 0;
        CustomProperties.TryDeferLoad("annex", "test", () => runs++);

        _now = CustomProperties.WaitSeconds + 1f;
        CustomProperties.Pump();

        Assert.Equal(0, runs);
        Assert.Equal(0, CustomProperties.DeferredLoadCount);
        Assert.Contains(_log, l => l.Level == "error" && l.Message.Contains("Gave up loading 'annex'"));
    }

    [Fact]
    public void ThrowingLoadIsLoggedAndOthersStillRun()
    {
        PropertyManager.Register(Def("annex"));
        PropertyManager.Register(Def("wing"));
        int runs = 0;
        CustomProperties.TryDeferLoad("annex", "first", () => throw new InvalidOperationException("boom"));
        CustomProperties.TryDeferLoad("wing", "second", () => runs++);

        _inWorld.Add("annex");
        _inWorld.Add("wing");
        CustomProperties.Pump();

        Assert.Equal(1, runs);
        Assert.Contains(_log, l => l.Level == "error" && l.Message.Contains("Deferred load of 'annex'") && l.Message.Contains("boom"));
    }

    // ---- heatmap ----

    [Fact]
    public void HeatmapStartsAtOnceWithoutRegisteredProperties()
    {
        Assert.False(CustomProperties.TryDeferHeatmap(() => { }));
    }

    [Fact]
    public void HeatmapWaitsForRegisteredPropertiesThenStartsOnce()
    {
        PropertyManager.Register(Def("annex"));
        int starts = 0;

        Assert.True(CustomProperties.TryDeferHeatmap(() => starts++));
        Assert.True(CustomProperties.HeatmapDeferred);
        CustomProperties.Pump();
        Assert.Equal(0, starts);

        _inWorld.Add("annex");
        CustomProperties.Pump();
        Assert.Equal(1, starts);
        Assert.False(CustomProperties.HeatmapDeferred);
    }

    [Fact]
    public void HeatmapStartIsLetThroughWhileItReplays()
    {
        PropertyManager.Register(Def("annex"));
        bool throughDuringReplay = false;
        CustomProperties.TryDeferHeatmap(() => throughDuringReplay = !CustomProperties.TryDeferHeatmap(() => { }));
        _inWorld.Add("annex");

        CustomProperties.Pump();

        Assert.True(throughDuringReplay);
    }

    [Fact]
    public void HeatmapStartsAfterTheDeadlineWithAWarning()
    {
        PropertyManager.Register(Def("annex"));
        int starts = 0;
        CustomProperties.TryDeferHeatmap(() => starts++);

        _now = CustomProperties.WaitSeconds + 1f;
        CustomProperties.Pump();

        Assert.Equal(1, starts);
        Assert.Contains(_log, l => l.Level == "warn" && l.Message.Contains("without annex"));
    }

    [Fact]
    public void ThrowingHeatmapStartIsLogged()
    {
        PropertyManager.Register(Def("annex"));
        CustomProperties.TryDeferHeatmap(() => throw new InvalidOperationException("boom"));
        _inWorld.Add("annex");

        CustomProperties.Pump();

        Assert.Contains(_log, l => l.Level == "error" && l.Message.Contains("Starting the heatmap failed"));
        Assert.False(CustomProperties.IsStartingHeatmap);
    }

    // ---- patches ----

    [Fact]
    public void PatchTargetsExistInTargetRuntime()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        Assert.NotNull(typeof(S1Property.PropertyManager).GetMethod("WriteData", flags, null, new[] { typeof(string) }, null));
        Assert.NotNull(typeof(S1Property.BusinessManager).GetMethod("WriteData", flags, null, new[] { typeof(string) }, null));
        Assert.NotNull(typeof(S1Loaders.PropertyLoader).GetMethod("Load", flags, null, new[] { typeof(string) }, null));
        Assert.NotNull(typeof(S1Loaders.PropertyLoader).GetMethod("Load", flags, null,
            new[] { typeof(S1Datas.PropertyData), typeof(string) }, null));
        Assert.NotNull(typeof(S1Heatmap.HeatmapManager).GetMethod("Start", flags, null, Type.EmptyTypes, null));
        Assert.NotNull(typeof(S1Heatmap.HeatmapManager).GetMethod("ToggleHeatmapActive", flags, null, new[] { typeof(S1Property.Property) }, null));
    }

    // ---- unfinished loads, heatmap cut-off, re-registration ----

    [Fact]
    public void PropertyWithPendingLoadKeepsItsFileAndIsNotSaved()
    {
        PropertyManager.Register(Def("annex", () => null));
        _inWorld.Add("annex");
        var file = WriteSave("Annex.json", "annex");
        Assert.True(CustomProperties.TryDeferLoad("annex", "test", () => { }));
        // The property is in the world but its root is not ready, so the load is pending.
        Assert.Equal(1, CustomProperties.DeferredLoadCount);
        Assert.True(CustomProperties.IsLoadUnfinished("annex"));
        Assert.True(CustomProperties.HasUnfinishedLoads());
        Assert.Equal(new[] { file }, Kept());
    }

    [Fact]
    public void PropertyWhoseLoadTimedOutKeepsItsFileUntilTheNextWorld()
    {
        PropertyManager.Register(Def("annex", () => null));
        _inWorld.Add("annex");
        var file = WriteSave("Annex.json", "annex");
        CustomProperties.TryDeferLoad("annex", "test", () => { });

        _now = CustomProperties.WaitSeconds + 1f;
        CustomProperties.Pump();

        Assert.Equal(0, CustomProperties.DeferredLoadCount);
        Assert.True(CustomProperties.IsLoadUnfinished("annex"));
        Assert.True(CustomProperties.HasUnfinishedLoads());
        Assert.Equal(new[] { file }, Kept());
        Assert.Contains(_log, l => l.Level == "error" && l.Message.Contains("not saved until the next load"));
    }

    [Fact]
    public void FinishedLoadIsSavedAgain()
    {
        PropertyManager.Register(Def("annex"));
        CustomProperties.TryDeferLoad("annex", "test", () => { });
        _inWorld.Add("annex");
        CustomProperties.Pump();

        Assert.False(CustomProperties.IsLoadUnfinished("annex"));
        Assert.False(CustomProperties.HasUnfinishedLoads());
        WriteSave("Annex.json", "annex");
        Assert.Empty(Kept());
    }

    [Fact]
    public void ReadyLoadsReplayInArrivalOrder()
    {
        PropertyManager.Register(Def("annex"));
        var order = new List<string>();
        CustomProperties.TryDeferLoad("annex", "property", () => order.Add("property"));
        CustomProperties.TryDeferLoad("annex", "business", () => order.Add("business"));
        _inWorld.Add("annex");

        CustomProperties.Pump();

        Assert.Equal(new[] { "property", "business" }, order);
    }

    [Fact]
    public void HeatmapStopsWaitingForAbsentPropertyOnceTheSaveLoaded()
    {
        PropertyManager.Register(Def("annex"));
        int starts = 0;
        CustomProperties.TryDeferHeatmap(() => starts++);

        CustomProperties.Pump();
        Assert.Equal(0, starts);

        CustomProperties.MarkLoadComplete();
        CustomProperties.Pump();

        Assert.Equal(1, starts);
        Assert.True(CustomProperties.IsMissingFromHeatmap("annex"));
        Assert.False(CustomProperties.IsMissingFromHeatmap("other"));
    }

    [Fact]
    public void HeatmapKeepsWaitingWhenTheSaveHasDataForTheProperty()
    {
        PropertyManager.Register(Def("annex"));
        int starts = 0;
        CustomProperties.TryDeferHeatmap(() => starts++);
        CustomProperties.TryDeferLoad("annex", "test", () => { });

        CustomProperties.MarkLoadComplete();
        CustomProperties.Pump();

        Assert.Equal(0, starts);
    }

    [Fact]
    public void DeferredHeatmapOfADestroyedHeatmapIsDropped()
    {
        PropertyManager.Register(Def("annex"));
        bool alive = true;
        int starts = 0;
        CustomProperties.TryDeferHeatmap(() => starts++, () => alive);
        alive = false;
        _inWorld.Add("annex");

        CustomProperties.Pump();

        Assert.Equal(0, starts);
        Assert.False(CustomProperties.HeatmapDeferred);
        Assert.Contains(_log, l => l.Level == "warn" && l.Message.Contains("no longer exists"));
    }

    [Fact]
    public void RegisteringTheSameCodeAgainReplacesTheDefinition()
    {
        PropertyManager.Register(Def("annex", () => null));
        _inWorld.Add("annex");
        Assert.True(CustomProperties.TryDeferLoad("annex", "before", () => { }));
        PropertyManager.Register(Def("annex"));

        Assert.Equal(1, CustomProperties.RegisteredCount);
        Assert.Contains(_log, l => l.Level == "info" && l.Message.Contains("registered again"));
        Assert.False(CustomProperties.TryDeferLoad("annex", "after", () => { }));
    }

    [Fact]
    public void PatchMethodsCarryHarmonyAttributes()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        foreach (var name in new[] { "PropertyWriteDataPostfix", "BusinessWriteDataPostfix" })
            Assert.NotNull(typeof(CustomPropertyPatches).GetMethod(name, flags)!.GetCustomAttribute<HarmonyPostfix>());
        foreach (var name in new[] { "LoadFolderPrefix", "LoadDataPrefix", "HeatmapStartPrefix", "ToggleHeatmapPrefix" })
            Assert.NotNull(typeof(CustomPropertyPatches).GetMethod(name, flags)!.GetCustomAttribute<HarmonyPrefix>());
        foreach (var name in new[] { "PropertyWriteDataPrefix", "BusinessWriteDataPrefix" })
            Assert.NotNull(typeof(CustomPropertyPatches).GetMethod(name, flags)!.GetCustomAttribute<HarmonyPrefix>());
        foreach (var name in new[] { "PropertyWriteDataFinalizer", "BusinessWriteDataFinalizer" })
            Assert.NotNull(typeof(CustomPropertyPatches).GetMethod(name, flags)!.GetCustomAttribute<HarmonyFinalizer>());
        Assert.Null(typeof(CustomPropertyPatches).GetMethod("ShouldSavePostfix", flags));
    }

    [Fact]
    public void SavedCodeIsReadFromLegacyFolders()
    {
        var folder = Path.Combine(_dir, "Legacy");
        Directory.CreateDirectory(folder);
        Assert.Null(CustomProperties.ReadSavedCode(folder));

        File.WriteAllText(Path.Combine(folder, "Business.json"), "{ \"PropertyCode\" : \"annex\" }");
        Assert.Equal("annex", CustomProperties.ReadSavedCode(folder));
    }
}
