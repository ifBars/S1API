#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1DeliveryUi = Il2CppScheduleOne.UI.Phone.Delivery;
using S1Property = Il2CppScheduleOne.Property;
using S1PropertyList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Property.Property>;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
using S1DeliveryUi = ScheduleOne.UI.Phone.Delivery;
using S1Property = ScheduleOne.Property;
using S1PropertyList = System.Collections.Generic.List<ScheduleOne.Property.Property>;
#endif

using System;
using HarmonyLib;
using S1API.Internal.Deliveries;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Connects the delivery app's destination list, the loading bay check and the delivery tick to
    /// <see cref="DeliveryDestinationRuntime"/>. The arrival hold itself runs in <see cref="DeliveryPatches"/>.
    /// </summary>
    [HarmonyPatch]
    internal static class DeliveryDestinationPatches
    {
        private static readonly Logging.Log Logger = new Logging.Log("DeliveryDestinationPatches");

        [HarmonyPatch(typeof(S1DeliveryUi.DeliveryShop), "GetPotentialDestinations")]
        [HarmonyPostfix]
        private static void GetPotentialDestinationsPostfix(S1PropertyList __result)
        {
            try
            {
                DeliveryDestinationRuntime.AppendDestinations(__result);
            }
            catch (Exception ex)
            {
                Logger.Error($"Registered delivery destinations could not be added to the delivery app: {ex}");
            }
        }

        [HarmonyPatch(typeof(S1Delivery.DeliveryManager), nameof(S1Delivery.DeliveryManager.IsLoadingBayFree))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool IsLoadingBayFreePrefix(S1Property.Property destination, int loadingDockIndex, ref bool __result)
        {
            try
            {
                if (!DeliveryDestinationRuntime.BlockBay(destination, loadingDockIndex))
                    return true;
                __result = false;
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Loading bay check for dock #{loadingDockIndex + 1} failed; vanilla runs it. {ex}");
                return true;
            }
        }

        [HarmonyPatch(typeof(S1Delivery.DeliveryManager), "OnTimePass")]
        [HarmonyPrefix]
        private static void OnTimePassPrefix(S1Delivery.DeliveryManager __instance)
        {
            try
            {
                DeliveryDestinationRuntime.BeforeTimePass(__instance);
            }
            catch (Exception ex)
            {
                Logger.Error($"Held delivery arrivals could not be prepared for the delivery tick: {ex}");
            }
        }

        [HarmonyPatch(typeof(S1Delivery.DeliveryManager), "OnTimePass")]
        [HarmonyPostfix]
        private static void OnTimePassPostfix(S1Delivery.DeliveryManager __instance)
        {
            try
            {
                DeliveryDestinationRuntime.AfterTimePass(__instance);
            }
            catch (Exception ex)
            {
                Logger.Error($"Held delivery arrivals could not be put back after the delivery tick: {ex}");
            }
        }
    }
}
