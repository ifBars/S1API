using HarmonyLib;
using S1API.Internal.Vehicles;

#if IL2CPPMELON
using S1Vehicles = Il2CppScheduleOne.Vehicles;
#else
using S1Vehicles = ScheduleOne.Vehicles;
#endif

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Hands the vehicle types registered through <c>VehicleRegistry.RegisterType</c> to each new
    /// vehicle manager, before the save's vehicles load by code.
    /// </summary>
    [HarmonyPatch(typeof(S1Vehicles.VehicleManager), nameof(S1Vehicles.VehicleManager.Awake))]
    internal static class VehicleTypePatches
    {
        [HarmonyPostfix]
        private static void Postfix(S1Vehicles.VehicleManager __instance)
        {
            VehicleTypeRuntime.Apply(__instance);
        }
    }
}
