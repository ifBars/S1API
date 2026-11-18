#if IL2CPPMELON
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
using S1Property = Il2CppScheduleOne.Property;
#elif MONOMELON
using S1Datas = ScheduleOne.Persistence.Datas;
using S1Property = ScheduleOne.Property;
#endif

using System;
using HarmonyLib;
using S1API.Internal.Properties;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Connects the content cull and the property load to <see cref="PropertyHooks"/>.
    /// </summary>
    [HarmonyPatch]
    internal static class PropertyHookPatches
    {
        private static readonly Logging.Log Logger = new Logging.Log("PropertyHookPatches");

        // Virtual, so the call from Property.UpdateCulling is not inlined past this patch.
        [HarmonyPatch(typeof(S1Property.Property), nameof(S1Property.Property.SetContentCulled))]
        [HarmonyPrefix]
        private static bool SetContentCulledPrefix(S1Property.Property __instance, bool culled)
        {
            try
            {
                return __instance == null || !PropertyHooks.RefuseCull(__instance.PropertyCode, culled);
            }
            catch (Exception ex)
            {
                Logger.Error($"Content cull check failed, vanilla culls: {ex}");
                return true;
            }
        }

        // LoadProperty applies the base property data and returns; PropertyLoader loads objects and employees after it.
        [HarmonyPatch(typeof(S1Property.PropertyManager), nameof(S1Property.PropertyManager.LoadProperty))]
        [HarmonyPostfix]
        private static void LoadPropertyPostfix(S1Property.PropertyManager __instance, S1Datas.PropertyData propertyData)
        {
            try
            {
                if (propertyData == null)
                {
                    Logger.Warning("LoadProperty ran without property data; OnPropertyLoaded not raised.");
                    return;
                }
                var property = __instance != null ? __instance.GetProperty(propertyData.PropertyCode) : null;
                if (property == null)
                {
                    Logger.Warning($"LoadProperty for '{propertyData.PropertyCode}' found no property; OnPropertyLoaded not raised.");
                    return;
                }
                int employees = propertyData.Employees != null ? propertyData.Employees.Length : 0;
                PropertyHooks.RaiseLoaded(property, employees);
            }
            catch (Exception ex)
            {
                Logger.Error($"OnPropertyLoaded failed: {ex}");
            }
        }
    }
}
