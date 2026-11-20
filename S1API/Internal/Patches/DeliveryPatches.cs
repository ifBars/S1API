#if IL2CPPMELON
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1DeliveryUi = Il2CppScheduleOne.UI.Phone.Delivery;
using S1Shop = Il2CppScheduleOne.UI.Shop;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
#elif MONOMELON
using S1Datas = ScheduleOne.Persistence.Datas;
using S1Delivery = ScheduleOne.Delivery;
using S1DeliveryUi = ScheduleOne.UI.Phone.Delivery;
using S1Shop = ScheduleOne.UI.Shop;
using S1Vehicles = ScheduleOne.Vehicles;
#endif

using HarmonyLib;
using S1API.Deliveries;
using S1API.Internal.Deliveries;
using S1API.Internal.Entities;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Adapts native delivery lifecycle, UI, and saved-vehicle calls to S1API delivery services.
    /// </summary>
    [HarmonyPatch]
    internal static class DeliveryPatches
    {
        private static readonly Logging.Log Logger = new Logging.Log("DeliveryPatches");

        [HarmonyPatch(typeof(S1DeliveryUi.DeliveryApp), "SetIsAvailable")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool DeliveryAppSetIsAvailablePrefix(
            S1DeliveryUi.DeliveryApp __instance,
            S1Shop.ShopInterface matchingShop,
            bool available)
        {
            if (!SupplierRuntimeCoordinator.IsOwnedShop(matchingShop))
                return true;

            if (!SupplierRuntimeCoordinator.EnsureDeliveryEntry(__instance, matchingShop, available))
            {
                Logger.Warning(
                    $"Delivery UI could not be provisioned for custom supplier shop '{matchingShop?.ShopName}'.");
            }

            return false;
        }

        [HarmonyPatch(typeof(S1DeliveryUi.DeliveryApp), "CreateDeliveryStatusDisplay")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool DeliveryAppCreateDeliveryStatusDisplayPrefix(
            S1DeliveryUi.DeliveryApp __instance,
            S1Delivery.DeliveryInstance __0)
        {
            return !SupplierRuntimeCoordinator.TryDeferDeliveryStatusDisplay(__instance, __0);
        }

        [HarmonyPatch(typeof(S1Delivery.DeliveryInstance), nameof(S1Delivery.DeliveryInstance.SetStatus))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool DeliverySetStatusPrefix(
            S1Delivery.DeliveryInstance __instance,
            S1Delivery.EDeliveryStatus status,
            out StatusPatchState __state)
        {
            DeliveryStatus previousStatus = DeliveryEventBridge.BeforeStatusChange(__instance);
            bool deferred = SupplierDeliveryRecovery.TryDeferStatus(__instance, status)
                || TryHoldArrival(__instance, status);
            __state = new StatusPatchState(previousStatus, deferred);
            return !deferred;
        }

        private static bool TryHoldArrival(S1Delivery.DeliveryInstance delivery, S1Delivery.EDeliveryStatus status)
        {
            try
            {
                return DeliveryDestinationRuntime.TryHold(delivery, status);
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Arrival hold check for delivery {delivery?.DeliveryID} failed; the delivery arrives normally. {ex}");
                return false;
            }
        }

        [HarmonyPatch(typeof(S1Delivery.DeliveryInstance), nameof(S1Delivery.DeliveryInstance.SetStatus))]
        [HarmonyPostfix]
        private static void DeliverySetStatusPostfix(
            S1Delivery.DeliveryInstance __instance,
            StatusPatchState __state)
        {
            if (!__state.Deferred)
                DeliveryEventBridge.AfterStatusChange(__instance, __state.PreviousStatus);
        }

        [HarmonyPatch(typeof(S1Vehicles.VehicleManager), nameof(S1Vehicles.VehicleManager.LoadVehicle))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool VehicleManagerLoadVehiclePrefix(S1Datas.VehicleData data, string path)
        {
            return !SupplierDeliveryRecovery.TryDeferVehicleLoad(data, path);
        }

        private readonly struct StatusPatchState
        {
            internal StatusPatchState(DeliveryStatus previousStatus, bool deferred)
            {
                PreviousStatus = previousStatus;
                Deferred = deferred;
            }

            internal DeliveryStatus PreviousStatus { get; }
            internal bool Deferred { get; }
        }
    }
}
