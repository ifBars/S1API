using System.Reflection;
using HarmonyLib;
using S1API.Internal.Entities;
using S1API.Internal.Patches;

#if IL2CPPMELON
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#elif MONOMELON
using S1NPCs = ScheduleOne.NPCs;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif

namespace S1API.Tests.Entities;

[Collection("NpcAttackEvents")]
public sealed class NpcPlayerEventTests : IDisposable
{
    private readonly List<(string Level, string Message)> _log = new();

    public NpcPlayerEventTests()
    {
        NpcAttackEvents.LogSinkForTesting = (level, message) => _log.Add((level, message));
        PlayerEvents.LogSinkForTesting = (level, message) => _log.Add((level, message));
    }

    public void Dispose()
    {
        NpcAttackEvents.LogSinkForTesting = null;
        PlayerEvents.LogSinkForTesting = null;
    }

    [Fact]
    public void PatchTargetsExistInTargetRuntime()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Assert.NotNull(NpcImpactPatches.TargetMethod());
        Assert.NotNull(typeof(S1NPCs.NPC).GetMethod("InitializeNPC", flags));
        Assert.NotNull(typeof(S1PlayerScripts.Player).GetMethod("ConsumeProduct", flags));
        Assert.NotNull(typeof(S1PlayerScripts.Player).GetMethod("ExitVehicle", flags));
        Assert.NotNull(typeof(S1PlayerScripts.Player).GetMethod("EnterVehicle", flags));
        Assert.NotNull(typeof(S1PlayerScripts.Player).GetMethod("CurrentVehicleChanged", flags));
#if MONOMELON
        Assert.NotNull(typeof(S1NPCs.NPC).GetField("impactHistory", flags));
#else
        Assert.NotNull(typeof(S1NPCs.NPC).GetProperty("impactHistory"));
#endif
    }

    [Fact]
    public void PatchMethodsCarryHarmonyAttributes()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        Assert.NotNull(typeof(NpcImpactPatches).GetMethod("ReceiveImpactPrefix", flags)!.GetCustomAttribute<HarmonyPrefix>());
        Assert.NotNull(typeof(NpcImpactPatches).GetMethod("ReceiveImpactPostfix", flags)!.GetCustomAttribute<HarmonyPostfix>());
        Assert.NotNull(typeof(NpcInitializePatches).GetMethod("InitializeNpcPrefix", flags)!.GetCustomAttribute<HarmonyPrefix>());
        Assert.NotNull(typeof(PlayerEventPatches).GetMethod("ConsumeProductPostfix", flags)!.GetCustomAttribute<HarmonyPostfix>());
        Assert.NotNull(typeof(PlayerEventPatches).GetMethod("ExitVehiclePrefix", flags)!.GetCustomAttribute<HarmonyPrefix>());
        Assert.NotNull(typeof(PlayerEventPatches).GetMethod("ExitVehiclePostfix", flags)!.GetCustomAttribute<HarmonyPostfix>());
        Assert.NotNull(typeof(PlayerEventPatches).GetMethod("EnterVehiclePostfix", flags)!.GetCustomAttribute<HarmonyPostfix>());
        Assert.NotNull(typeof(PlayerEventPatches).GetMethod("CurrentVehicleChangedPostfix", flags)!.GetCustomAttribute<HarmonyPostfix>());
    }

    [Fact]
    public void ThrowingPlayerHandlerIsLoggedAndOthersRun()
    {
        int calls = 0;
        Action<int> handlers = _ => throw new InvalidOperationException("boom");
        handlers += _ => calls++;

        PlayerEvents.Invoke(handlers, "OnTest", h => ((Action<int>)h)(1));

        Assert.Equal(1, calls);
        Assert.Single(_log, l => l.Level == "error" && l.Message.Contains("A handler of OnTest threw") && l.Message.Contains("boom"));
    }

    [Fact]
    public void InitializingMugshotIsMutableAndHandlersRunInOrder()
    {
        var args = new global::S1API.Entities.NpcInitializingArgs("npc_a", null);
        var order = new List<int>();
        global::S1API.Entities.NPC.OnInitializing += First;
        global::S1API.Entities.NPC.OnInitializing += Second;
        try
        {
            global::S1API.Entities.NPC.RaiseInitializing(args);
        }
        finally
        {
            global::S1API.Entities.NPC.OnInitializing -= First;
            global::S1API.Entities.NPC.OnInitializing -= Second;
        }

        Assert.Equal(new[] { 1, 2 }, order);
        Assert.Equal("npc_a", args.Id);
        Assert.Contains(_log, l => l.Level == "info" && l.Message.Contains("npc_a initializing"));

        void First(global::S1API.Entities.NpcInitializingArgs a) => order.Add(1);
        void Second(global::S1API.Entities.NpcInitializingArgs a) => order.Add(2);
    }

    [Fact]
    public void ThrowingInitializingHandlerDoesNotStopOthers()
    {
        int calls = 0;
        global::S1API.Entities.NPC.OnInitializing += Bad;
        global::S1API.Entities.NPC.OnInitializing += Good;
        try
        {
            global::S1API.Entities.NPC.RaiseInitializing(new global::S1API.Entities.NpcInitializingArgs("npc_b", null));
        }
        finally
        {
            global::S1API.Entities.NPC.OnInitializing -= Bad;
            global::S1API.Entities.NPC.OnInitializing -= Good;
        }

        Assert.Equal(1, calls);
        Assert.Single(_log, l => l.Level == "error" && l.Message.Contains("OnInitializing of npc_b"));

        void Bad(global::S1API.Entities.NpcInitializingArgs a) => throw new InvalidOperationException("boom");
        void Good(global::S1API.Entities.NpcInitializingArgs a) => calls++;
    }

    [Fact]
    public void PublicSurfaceExists()
    {
        var npc = typeof(global::S1API.Entities.NPC);
        var player = typeof(global::S1API.Entities.Player);
        Assert.NotNull(npc.GetEvent("OnImpact"));
        Assert.NotNull(npc.GetEvent("OnInitializing", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(player.GetEvent("OnProductConsumed"));
        Assert.NotNull(player.GetEvent("OnExitingVehicle"));
        Assert.NotNull(player.GetEvent("OnVehicleChanged"));
        Assert.Equal(typeof(global::S1API.Entities.NpcImpact), npc.GetEvent("OnImpact")!.EventHandlerType!.GetGenericArguments()[0]);
    }
}
