using System.Reflection;
using HarmonyLib;
using S1API.Internal.Patches;
using S1API.Internal.Properties;
using S1API.Property;

#if IL2CPPMELON
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
using S1Property = Il2CppScheduleOne.Property;
#elif MONOMELON
using S1Datas = ScheduleOne.Persistence.Datas;
using S1Property = ScheduleOne.Property;
#endif

namespace S1API.Tests.Property;

[Collection("PropertyHooks")]
public sealed class PropertyHookTests : IDisposable
{
    private readonly List<(string Level, string Message)> _log = new();

    public PropertyHookTests()
    {
        PropertyHooks.ResetForTesting();
        PropertyHooks.LogSinkForTesting = (level, message) => _log.Add((level, message));
    }

    public void Dispose() => PropertyHooks.ResetForTesting();

    [Fact]
    public void PatchTargetsExistInTargetRuntime()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        var cull = typeof(S1Property.Property).GetMethod("SetContentCulled", flags, null, new[] { typeof(bool) }, null);
        Assert.NotNull(cull);
        Assert.True(cull!.IsVirtual);
        Assert.NotNull(typeof(S1Property.PropertyManager).GetMethod("LoadProperty", flags, null,
            new[] { typeof(S1Datas.PropertyData), typeof(string) }, null));
    }

    [Fact]
    public void PatchMethodsCarryHarmonyAttributes()
    {
        var cull = typeof(PropertyHookPatches).GetMethod("SetContentCulledPrefix", BindingFlags.Static | BindingFlags.NonPublic);
        var load = typeof(PropertyHookPatches).GetMethod("LoadPropertyPostfix", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(cull!.GetCustomAttribute<HarmonyPrefix>());
        Assert.NotNull(load!.GetCustomAttribute<HarmonyPostfix>());
    }

    [Fact]
    public void UnlockedPropertyCullsAsVanilla()
    {
        Assert.False(PropertyHooks.RefuseCull("rv", true));
        Assert.False(PropertyHooks.RefuseCull("rv", false));
    }

    [Fact]
    public void LockedPropertyRefusesCullButNotShow()
    {
        PropertyHooks.SetCullingLocked("rv", true);

        Assert.True(PropertyHooks.IsCullingLocked("rv"));
        Assert.True(PropertyHooks.RefuseCull("rv", true));
        Assert.True(PropertyHooks.RefuseCull("rv", true));
        Assert.False(PropertyHooks.RefuseCull("rv", false));
        Assert.False(PropertyHooks.RefuseCull("manor", true));
        Assert.Single(_log, l => l.Message == "Content of 'rv' stays shown; culling is locked.");
    }

    [Fact]
    public void UnlockRestoresVanillaCulling()
    {
        PropertyHooks.SetCullingLocked("rv", true);
        PropertyHooks.SetCullingLocked("rv", false);

        Assert.False(PropertyHooks.RefuseCull("rv", true));
        Assert.Contains(_log, l => l.Message == "Content culling of 'rv' unlocked.");
    }

    [Fact]
    public void LockWithoutCodeIsIgnoredWithWarning()
    {
        PropertyHooks.SetCullingLocked("", true);

        Assert.Contains(_log, l => l.Level == "warn");
        Assert.False(PropertyHooks.IsCullingLocked(""));
    }

    [Fact]
    public void LoadedEventIsExposedOnPropertyManager()
    {
        var evt = typeof(PropertyManager).GetEvent("OnPropertyLoaded");
        Assert.NotNull(evt);
        Assert.Equal(typeof(Action<PropertyWrapper, int>), evt!.EventHandlerType);
    }

    [Fact]
    public void RaiseLoadedWithoutPropertyDoesNothing()
    {
        bool called = false;
        PropertyManager.OnPropertyLoaded += (_, _) => called = true;

        PropertyHooks.RaiseLoaded(null!, 3);

        Assert.False(called);
    }
}
