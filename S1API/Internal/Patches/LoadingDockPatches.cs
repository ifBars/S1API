#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
using S1Vehicles = ScheduleOne.Vehicles;
#endif

using HarmonyLib;
using S1API.Internal.Deliveries;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Observes native loading-dock transitions without replacing their behavior.
    /// </summary>
    [HarmonyPatch(typeof(S1Delivery.LoadingDock))]
    internal static class LoadingDockPatches
    {
        [HarmonyPatch("SetOccupant")]
        [HarmonyPrefix]
        private static void SetOccupantPrefix(
            S1Delivery.LoadingDock __instance,
            out S1Vehicles.LandVehicle? __state)
        {
            __state = __instance.DynamicOccupant;
        }

        [HarmonyPatch("SetOccupant")]
        [HarmonyPostfix]
        private static void SetOccupantPostfix(
            S1Delivery.LoadingDock __instance,
            S1Vehicles.LandVehicle? __state)
        {
            LoadingDockEventBridge.NotifyDynamicOccupantChanged(
                __instance,
                __state,
                __instance.DynamicOccupant);
        }

        [HarmonyPatch(nameof(S1Delivery.LoadingDock.SetStaticOccupant))]
        [HarmonyPrefix]
        private static void SetStaticOccupantPrefix(
            S1Delivery.LoadingDock __instance,
            out S1Vehicles.LandVehicle? __state)
        {
            __state = __instance.StaticOccupant;
        }

        [HarmonyPatch(nameof(S1Delivery.LoadingDock.SetStaticOccupant))]
        [HarmonyPostfix]
        private static void SetStaticOccupantPostfix(
            S1Delivery.LoadingDock __instance,
            S1Vehicles.LandVehicle? __state)
        {
            LoadingDockEventBridge.NotifyStaticOccupantChanged(
                __instance,
                __state,
                __instance.StaticOccupant);
        }

        [HarmonyPatch(nameof(S1Delivery.LoadingDock.IsAcceptingItems), MethodType.Setter)]
        [HarmonyPrefix]
        private static void SetAcceptingItemsPrefix(
            S1Delivery.LoadingDock __instance,
            out bool __state)
        {
            __state = __instance.IsAcceptingItems;
        }

        [HarmonyPatch(nameof(S1Delivery.LoadingDock.IsAcceptingItems), MethodType.Setter)]
        [HarmonyPostfix]
        private static void SetAcceptingItemsPostfix(
            S1Delivery.LoadingDock __instance,
            bool __state)
        {
            LoadingDockEventBridge.NotifyAcceptingItemsChanged(
                __instance,
                __state,
                __instance.IsAcceptingItems);
        }
    }
}
