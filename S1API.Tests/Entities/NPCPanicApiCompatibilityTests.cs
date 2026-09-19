#if IL2CPPMELON
using NativeNpc = Il2CppScheduleOne.NPCs.NPC;
#elif MONOMELON
using NativeNpc = ScheduleOne.NPCs.NPC;
#endif

using System.Reflection;
using S1API.Entities;

namespace S1API.Tests.Entities;

public sealed class NPCPanicApiCompatibilityTests
{
    [Fact]
    public void PanicRetainsItsParameterlessManagedSurface()
    {
        MethodInfo? method = typeof(NPC).GetMethod(
            nameof(NPC.Panic),
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NativePanicRpcRemainsParameterless()
    {
        MethodInfo? method = typeof(NativeNpc).GetMethod(
            "SetPanicked_Server",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }
}
