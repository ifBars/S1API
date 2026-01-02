using HarmonyLib;
using S1API.Vehicles;

#if (IL2CPPMELON || Il2CppBepInEx)
using S1Vehicles = Il2CppScheduleOne.Vehicles;
#else
using S1Vehicles = ScheduleOne.Vehicles;
#endif

namespace S1API.Internal.Patches
{
    [HarmonyPatch(typeof(S1Vehicles.LandVehicle))]
    internal class LandVehiclePatches
    {
        [HarmonyPatch("OnDestroy")]
        [HarmonyPostfix]
        public static void OnDestroy(S1Vehicles.LandVehicle __instance) {
            VehicleRegistry.RemoveVehicle(__instance.GUID.ToString());
        }

    }
}
