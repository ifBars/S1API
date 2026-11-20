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
public sealed class NpcAttackEventTests : IDisposable
{
    private readonly List<(string Level, string Message)> _log = new();

    public NpcAttackEventTests() => NpcAttackEvents.LogSinkForTesting = (level, message) => _log.Add((level, message));

    public void Dispose() => NpcAttackEvents.LogSinkForTesting = null;

    [Fact]
    public void PatchTargetsExistInTargetRuntime()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Assert.NotNull(typeof(S1NPCs.NPCHealth).GetMethod("NotifyAttackedByPlayer", flags, null, new[] { typeof(S1PlayerScripts.Player) }, null));
        Assert.NotNull(typeof(S1NPCs.NPCHealth).GetMethod("KnockOut", flags, null, Type.EmptyTypes, null));
#if MONOMELON
        Assert.NotNull(typeof(S1NPCs.NPCHealth).GetField("_npc", flags));
#else
        Assert.NotNull(typeof(S1NPCs.NPCHealth).GetProperty("_npc"));
#endif
    }

    [Fact]
    public void PatchMethodsCarryHarmonyAttributes()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        Assert.NotNull(typeof(NpcAttackPatches).GetMethod("NotifyAttackedByPlayerPostfix", flags)!.GetCustomAttribute<HarmonyPostfix>());
        Assert.NotNull(typeof(NpcAttackPatches).GetMethod("KnockOutPostfix", flags)!.GetCustomAttribute<HarmonyPostfix>());
    }

    [Fact]
    public void ThrowingHandlerIsLoggedAndOthersRun()
    {
        int calls = 0;
        Action<int> handlers = _ => throw new InvalidOperationException("boom");
        handlers += _ => calls++;

        NpcAttackEvents.Invoke(handlers, "OnTest", h => ((Action<int>)h)(1));

        Assert.Equal(1, calls);
        Assert.Single(_log, l => l.Level == "error" && l.Message.Contains("A handler of OnTest threw") && l.Message.Contains("boom"));
    }

    [Fact]
    public void NoHandlersDoesNothing()
    {
        NpcAttackEvents.Invoke(null, "OnTest", _ => throw new InvalidOperationException("never"));
        Assert.Empty(_log);
    }

    [Fact]
    public void PublicSurfaceExists()
    {
        var npc = typeof(global::S1API.Entities.NPC);
        Assert.NotNull(npc.GetEvent("OnAttackedByPlayer"));
        Assert.NotNull(npc.GetEvent("OnKnockedOut"));
        Assert.NotNull(npc.GetEvent("OnAnyAttackedByPlayer", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(npc.GetEvent("OnAnyKnockedOut", BindingFlags.Public | BindingFlags.Static));
        Assert.Equal(typeof(global::S1API.Entities.Player), npc.GetProperty("LastAttacker")!.PropertyType);
    }
}
