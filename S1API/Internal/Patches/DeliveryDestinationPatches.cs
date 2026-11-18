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
    /// Connects the delivery app's destination and dock lists, the loading bay check, the delivery state change and
    /// the delivery tick to <see cref="DeliveryDestinationRuntime"/>. The arrival hold itself runs in
    /// <see cref="DeliveryPatches"/> and in <see cref="SetDeliveryStatePrefix"/>.
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

        [HarmonyPatch(typeof(S1DeliveryUi.DeliveryShop), nameof(S1DeliveryUi.DeliveryShop.RefreshLoadingDockUI))]
        [HarmonyPrefix]
        private static void RefreshLoadingDockUIPrefix() => DeliveryDestinationRuntime.BeginDockUiRefresh();

        /// <summary>Runs also when vanilla threw, so the refresh counter always goes back down.</summary>
        [HarmonyPatch(typeof(S1DeliveryUi.DeliveryShop), nameof(S1DeliveryUi.DeliveryShop.RefreshLoadingDockUI))]
        [HarmonyFinalizer]
        private static Exception? RefreshLoadingDockUIFinalizer(S1DeliveryUi.DeliveryShop __instance, Exception? __exception)
        {
            try
            {
                DeliveryDestinationRuntime.EndDockUiRefresh(__exception == null ? __instance : null);
            }
            catch (Exception ex)
            {
                Logger.Error($"Reserved loading docks could not be hidden in the delivery app: {ex}");
            }

            return __exception;
        }

        // DeliveryShop.LoadingDockDropdownSelected is not patched: it is a one-line field setter, and in IL2CPP a detour
        // on such a body can also catch other methods with the same native code. EndDockUiRefresh adds a dropdown
        // listener instead.

        [HarmonyPatch(typeof(S1DeliveryUi.DeliveryShop), nameof(S1DeliveryUi.DeliveryShop.SubmitOrder))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool SubmitOrderPrefix(S1DeliveryUi.DeliveryShop __instance)
        {
            try
            {
                return !DeliveryDestinationRuntime.BlockOrder(__instance);
            }
            catch (Exception ex)
            {
                Logger.Error($"Delivery order dock check failed; vanilla places the order. {ex}");
                return true;
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

        /// <summary>
        /// Holds an arrival before vanilla sends it to the clients, shows the notification and activates the van.
        /// </summary>
        [HarmonyPatch(typeof(S1Delivery.DeliveryManager), "SetDeliveryState")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool SetDeliveryStatePrefix(S1Delivery.DeliveryManager __instance, string deliveryID, S1Delivery.EDeliveryStatus status)
        {
            try
            {
                return !DeliveryDestinationRuntime.HoldStateChange(__instance, deliveryID, status);
            }
            catch (Exception ex)
            {
                Logger.Error($"Arrival hold check for delivery {deliveryID} failed; the state change runs normally. {ex}");
                return true;
            }
        }

        [HarmonyPatch(typeof(S1Delivery.DeliveryManager), "SetDeliveryState")]
        [HarmonyFinalizer]
        private static Exception? SetDeliveryStateFinalizer(Exception? __exception)
        {
            DeliveryDestinationRuntime.AfterStateChange();
            return __exception;
        }

        [HarmonyPatch(typeof(S1Delivery.DeliveryManager), "OnTimePass")]
        [HarmonyPrefix]
        private static void OnTimePassPrefix(S1Delivery.DeliveryManager __instance, out DeliveryDestinationRuntime.TickState __state)
        {
            __state = new DeliveryDestinationRuntime.TickState();
            try
            {
                DeliveryDestinationRuntime.BeforeTimePass(__instance, __state);
            }
            catch (Exception ex)
            {
                Logger.Error($"Held delivery arrivals could not be prepared for the delivery tick: {ex}");
            }
        }

        /// <summary>
        /// Runs after the tick also when it threw, so the deliveries the prefix took out always go back.
        /// Returns the tick's own exception unchanged, so Harmony rethrows it.
        /// </summary>
        [HarmonyPatch(typeof(S1Delivery.DeliveryManager), "OnTimePass")]
        [HarmonyFinalizer]
        private static Exception? OnTimePassFinalizer(
            S1Delivery.DeliveryManager __instance,
            DeliveryDestinationRuntime.TickState? __state,
            Exception? __exception)
        {
            try
            {
                DeliveryDestinationRuntime.AfterTimePass(__instance, __state, __exception);
            }
            catch (Exception ex)
            {
                Logger.Error($"Held delivery arrivals could not be put back after the delivery tick: {ex}");
            }

            return __exception;
        }
    }
}
