using HarmonyLib;
using S1API.Vehicles;
using S1API.Entities.Schedule;

#if IL2CPPMELON
using S1Vehicles = Il2CppScheduleOne.Vehicles;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#else
using S1Vehicles = ScheduleOne.Vehicles;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
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

        [HarmonyPatch("SetSeatOccupant")]
        [HarmonyPrefix]
        private static void SetSeatOccupantPrefix(
            S1Vehicles.LandVehicle __instance,
            int seatIndex,
            out SeatOccupancyState __state)
        {
            __state = default;
            if (__instance == null || __instance.Seats == null
                || seatIndex < 0 || seatIndex >= __instance.Seats.Length)
            {
                return;
            }

            var seat = __instance.Seats[seatIndex];
            if (seat != null)
                __state = new SeatOccupancyState(seatIndex, seat.Occupant);
        }

        [HarmonyPatch("SetSeatOccupant")]
        [HarmonyPostfix]
        private static void SetSeatOccupantPostfix(
            S1Vehicles.LandVehicle __instance,
            SeatOccupancyState __state)
        {
            if (!__state.IsValid || __instance == null || __instance.Seats == null
                || __state.Index >= __instance.Seats.Length)
            {
                return;
            }

            var seat = __instance.Seats[__state.Index];
            if (seat == null)
                return;

            VehicleRegistry.Wrap(__instance)?.NotifySeatOccupantChanged(
                __state.Index,
                __state.Occupant,
                seat.Occupant);
        }

        private readonly struct SeatOccupancyState
        {
            internal SeatOccupancyState(int index, S1PlayerScripts.Player? occupant)
            {
                Index = index;
                Occupant = occupant;
                IsValid = true;
            }

            internal int Index { get; }
            internal S1PlayerScripts.Player? Occupant { get; }
            internal bool IsValid { get; }
        }
    }
}
