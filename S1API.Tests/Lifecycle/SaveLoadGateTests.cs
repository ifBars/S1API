using System.Collections;
using System.Reflection;
using HarmonyLib;
using S1API.Internal.Lifecycle;
using S1API.Internal.Patches;
using S1API.Lifecycle;

#if IL2CPPMELON
using S1Persistence = Il2CppScheduleOne.Persistence;
#elif MONOMELON
using S1Persistence = ScheduleOne.Persistence;
#endif

namespace S1API.Tests.Lifecycle;

[Collection("SaveLoadGates")]
public sealed class SaveLoadGateTests : IDisposable
{
    private readonly List<(string Level, string Message)> _log = new();
    private float _now;
    private IEnumerator? _wait;

    public SaveLoadGateTests()
    {
        SaveLoadGates.ResetForTesting();
        SaveLoadGates.LogSinkForTesting = (level, message) => _log.Add((level, message));
        SaveLoadGates.ClockForTesting = () => _now;
        SaveLoadGates.StartWaitForTesting = routine => _wait = routine;
    }

    public void Dispose() => SaveLoadGates.ResetForTesting();

    [Fact]
    public void PatchTargetsExistInTargetRuntime()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        Assert.NotNull(typeof(S1Persistence.SaveManager).GetMethod("Save", flags, null, Type.EmptyTypes, null));
        Assert.NotNull(typeof(S1Persistence.SaveManager).GetMethod("Save", flags, null, new[] { typeof(string) }, null));
        Assert.NotNull(typeof(S1Persistence.LoadManager).GetMethod("StartGame", flags, null,
            new[] { typeof(S1Persistence.SaveInfo), typeof(bool), typeof(bool) }, null));
        Assert.NotNull(typeof(S1Persistence.LoadManager).GetMethod("LoadAsClient", flags, null, new[] { typeof(string) }, null));
    }

    [Fact]
    public void GatePrefixesRunFirst()
    {
        foreach (var name in new[] { "SavePrefix", "SaveToFolderPrefix", "StartGamePrefix", "LoadAsClientPrefix" })
        {
            var method = typeof(SaveLoadGatePatches).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var priority = method!.GetCustomAttribute<HarmonyPriority>();
            Assert.NotNull(priority);
            Assert.Equal(Priority.First, priority!.info.priority);
        }
    }

    [Fact]
    public void ExistingSavePostfixesSkipABlockedSave()
    {
        var postfixes = new[]
        {
            typeof(GenericSaveablesPatches).GetMethod("SaveManager_Save_Postfix", BindingFlags.Static | BindingFlags.NonPublic),
            typeof(QuestPatches).GetMethod("SaveManager_Save_Postfix", BindingFlags.Static | BindingFlags.NonPublic),
            typeof(CustomProductSavePatches).GetMethod("SavePostfix", BindingFlags.Static | BindingFlags.NonPublic),
        };
        foreach (var postfix in postfixes)
        {
            Assert.NotNull(postfix);
            Assert.Contains(postfix!.GetParameters(), p => p.Name == "__runOriginal" && p.ParameterType == typeof(bool));
        }
    }

    [Fact]
    public void SaveThroughShortOverloadAsksVetoesOnce()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var patches = typeof(SaveLoadGatePatches);
        int asked = 0;
        GameLifecycle.AddSaveVeto("Counter", () => { asked++; return null; });

        Assert.True((bool)patches.GetMethod("SavePrefix", flags)!.Invoke(null, null)!);
        Assert.True((bool)patches.GetMethod("SaveToFolderPrefix", flags)!.Invoke(null, null)!);
        patches.GetMethod("SaveFinalizer", flags)!.Invoke(null, null);
        Assert.Equal(1, asked);

        Assert.True((bool)patches.GetMethod("SaveToFolderPrefix", flags)!.Invoke(null, null)!);
        Assert.Equal(2, asked);
    }

    [Fact]
    public void VetoThatDisposesAnotherDoesNotSkipOrCrash()
    {
        IDisposable? second = null;
        GameLifecycle.AddSaveVeto("First", () => { second?.Dispose(); return null; });
        second = GameLifecycle.AddSaveVeto("Second", () => "never asked");

        Assert.True(SaveLoadGates.AllowSave("Save"));
    }

    [Fact]
    public void ThrowingVetoIsLoggedAgainForTheNextLoad()
    {
        GameLifecycle.AddSaveVeto("Faulty", () => throw new InvalidOperationException("boom"));
        SaveLoadGates.AllowSave("Save");
        SaveLoadGates.TryDefer("StartGame", () => { });
        SaveLoadGates.AllowSave("Save");

        Assert.Equal(2, _log.Count(l => l.Level == "error" && l.Message.StartsWith("Save veto from Faulty threw")));
    }

    [Fact]
    public void SaveRunsWithoutVetoes()
    {
        Assert.True(SaveLoadGates.AllowSave("Save"));
    }

    [Fact]
    public void VetoWithReasonBlocksAndRaisesEvent()
    {
        string? blocked = null;
        GameLifecycle.OnSaveBlocked += (owner, reason) => blocked = owner + ": " + reason;
        GameLifecycle.AddSaveVeto("Allow", () => null);
        GameLifecycle.AddSaveVeto("Broken", () => "the expansion failed");

        Assert.False(SaveLoadGates.AllowSave("Save"));
        Assert.Equal("Broken: the expansion failed", blocked);
        Assert.Contains(_log, l => l.Level == "warn" && l.Message == "Save blocked by Broken: the expansion failed");
    }

    [Fact]
    public void DisposedVetoNoLongerBlocks()
    {
        var veto = GameLifecycle.AddSaveVeto("Mod", () => "busy");
        Assert.False(SaveLoadGates.AllowSave("Save"));

        veto.Dispose();
        veto.Dispose();

        Assert.True(SaveLoadGates.AllowSave("Save"));
        Assert.Single(_log, l => l.Message == "Save veto from Mod removed.");
    }

    [Fact]
    public void ThrowingVetoIsLoggedOnceAndSkipped()
    {
        GameLifecycle.AddSaveVeto("Faulty", () => throw new InvalidOperationException("boom"));

        Assert.True(SaveLoadGates.AllowSave("Save"));
        Assert.True(SaveLoadGates.AllowSave("Save"));
        Assert.Single(_log, l => l.Level == "error" && l.Message.StartsWith("Save veto from Faulty threw"));
    }

    [Fact]
    public void ThrowingSaveBlockedHandlerIsLogged()
    {
        GameLifecycle.OnSaveBlocked += (_, _) => throw new InvalidOperationException("handler");
        GameLifecycle.AddSaveVeto("Mod", () => "busy");

        Assert.False(SaveLoadGates.AllowSave("Save"));
        Assert.Contains(_log, l => l.Level == "error" && l.Message.StartsWith("OnSaveBlocked handler"));
    }

    [Fact]
    public void LoadRunsAtOnceWhenGatesAreOpen()
    {
        GameLifecycle.AddLoadGate("Mod", () => true);

        Assert.False(SaveLoadGates.TryDefer("StartGame", () => throw new InvalidOperationException("must not resume")));
        Assert.Null(_wait);
    }

    [Fact]
    public void ClosedGateDefersAndResumesOnce()
    {
        bool ready = false;
        int runs = 0;
        GameLifecycle.AddLoadGate("Capture", () => ready);

        Assert.True(SaveLoadGates.TryDefer("StartGame", () => runs++));
        Assert.NotNull(_wait);
        Assert.Contains(_log, l => l.Message == "StartGame deferred until the load gate of Capture opens.");

        _now = 2f;
        Assert.False(SaveLoadGates.Poll(_now));
        Assert.Equal(0, runs);

        ready = true;
        _now = 3f;
        Assert.True(SaveLoadGates.Poll(_now));
        Assert.Equal(1, runs);
        Assert.False(SaveLoadGates.HasPendingLoad);
        Assert.Contains(_log, l => l.Message == "Deferred StartGame starts now (gates open after 3.0 s).");
    }

    [Fact]
    public void ResumedCallIsNotDeferredAgain()
    {
        bool ready = false;
        bool? deferredAgain = null;
        GameLifecycle.AddLoadGate("Capture", () => ready);
        Assert.True(SaveLoadGates.TryDefer("StartGame", () => deferredAgain = SaveLoadGates.TryDefer("StartGame", () => { })));

        ready = true;
        SaveLoadGates.Poll(1f);

        Assert.False(deferredAgain);
    }

    [Fact]
    public void GateTimesOutWithErrorAndLoadContinues()
    {
        int runs = 0;
        GameLifecycle.AddLoadGate("Stuck", () => false, 5f);
        Assert.True(SaveLoadGates.TryDefer("LoadAsClient", () => runs++));

        Assert.False(SaveLoadGates.Poll(4.9f));
        Assert.True(SaveLoadGates.Poll(5f));

        Assert.Equal(1, runs);
        Assert.Contains(_log, l => l.Level == "error" && l.Message == "Load gate from Stuck did not open within 5 s. The load continues without it.");
        Assert.Contains(_log, l => l.Message.StartsWith("Deferred LoadAsClient starts now (waited 5.0 s, 1 gate(s) timed out)"));
    }

    [Fact]
    public void DisposingTheLastClosedGateResumes()
    {
        int runs = 0;
        var gate = GameLifecycle.AddLoadGate("Mod", () => false);
        Assert.True(SaveLoadGates.TryDefer("StartGame", () => runs++));

        gate.Dispose();
        Assert.True(SaveLoadGates.Poll(0.5f));
        Assert.Equal(1, runs);
    }

    [Fact]
    public void ThrowingGateCountsAsOpen()
    {
        GameLifecycle.AddLoadGate("Faulty", () => throw new InvalidOperationException("boom"));

        Assert.False(SaveLoadGates.TryDefer("StartGame", () => { }));
        Assert.Single(_log, l => l.Level == "error" && l.Message.StartsWith("Load gate from Faulty threw"));
    }

    [Fact]
    public void SecondDeferredLoadReplacesTheFirst()
    {
        string? ran = null;
        bool ready = false;
        GameLifecycle.AddLoadGate("Mod", () => ready);
        SaveLoadGates.TryDefer("StartGame", () => ran = "first");
        SaveLoadGates.TryDefer("LoadAsClient", () => ran = "second");

        ready = true;
        SaveLoadGates.Poll(1f);

        Assert.Equal("second", ran);
        Assert.Contains(_log, l => l.Level == "warn" && l.Message == "LoadAsClient replaces the deferred StartGame.");
    }

    [Fact]
    public void ThrowingResumeIsLogged()
    {
        bool ready = false;
        GameLifecycle.AddLoadGate("Mod", () => ready);
        SaveLoadGates.TryDefer("StartGame", () => throw new InvalidOperationException("load"));

        ready = true;
        Assert.True(SaveLoadGates.Poll(1f));
        Assert.Contains(_log, l => l.Level == "error" && l.Message.StartsWith("Deferred StartGame failed"));
    }

    [Fact]
    public void InvalidArgumentsThrow()
    {
        Assert.Throws<ArgumentException>(() => GameLifecycle.AddSaveVeto(" ", () => null));
        Assert.Throws<ArgumentNullException>(() => GameLifecycle.AddSaveVeto("Mod", null!));
        Assert.Throws<ArgumentException>(() => GameLifecycle.AddLoadGate("", () => true));
        Assert.Throws<ArgumentNullException>(() => GameLifecycle.AddLoadGate("Mod", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => GameLifecycle.AddLoadGate("Mod", () => true, 0f));
    }
}
