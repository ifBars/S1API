using HarmonyLib;
using S1API.Vehicles;
using S1API.Entities.Schedule;

#if (IL2CPPMELON || IL2CPPBEPINEX)
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
            try
            {
                if (__instance != null)
                {
                    VehicleRegistry.RemoveVehicle(__instance.GUID.ToString());
                    DriveToCarParkSpec.VehiclesAtNoSpotLots.Remove(__instance);
                }
            }
            catch
            {
                // Ignore errors during cleanup - VehicleManager may be destroyed during scene unload
            }
        }

        /// <summary>
        /// Prevents the game from hiding vehicles that were assigned to parking lots with no
        /// parking spots. When spotIndex is invalid, the game calls SetVisible(false). This
        /// prefix intercepts that call and keeps the vehicle visible instead, leaving it
        /// wherever it currently is (e.g. where the NPC drove it).
        /// </summary>
        [HarmonyPatch(nameof(S1Vehicles.LandVehicle.SetVisible))]
        [HarmonyPrefix]
        public static bool SetVisible_Prefix(S1Vehicles.LandVehicle __instance, ref bool vis)
        {
            try
            {
                if (!vis && __instance != null && DriveToCarParkSpec.VehiclesAtNoSpotLots.Contains(__instance))
                {
                    vis = true;
                }
            }
            catch
            {
                // Don't break game visibility if our fix-up fails
            }
            return true;
        }
    }
}
