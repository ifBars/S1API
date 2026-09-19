#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.UI.Phone.Delivery;
using S1DeliveryModel = Il2CppScheduleOne.Delivery;
using S1Economy = Il2CppScheduleOne.Economy;
using S1Shop = Il2CppScheduleOne.UI.Shop;
#elif MONOMELON
using S1Delivery = ScheduleOne.UI.Phone.Delivery;
using S1DeliveryModel = ScheduleOne.Delivery;
using S1Economy = ScheduleOne.Economy;
using S1Shop = ScheduleOne.UI.Shop;
#endif

using System;
using S1API.Entities.Supplier;
using S1API.Internal.Deliveries;
using S1API.Internal.Entities.Suppliers;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;

namespace S1API.Internal.Entities
{
    /// <summary>
    /// Coordinates supplier lifecycle services without owning their implementation details.
    /// </summary>
    internal static class SupplierRuntimeCoordinator
    {
        private static readonly Log Logger = new Log("SupplierRuntimeCoordinator");

        internal static bool DeliveryPrefabsReadyForLocalProcess =>
            SupplierDeliveryVehicleRuntime.PrefabsReadyForLocalProcess;

        internal static void EnsurePrefabInfrastructure(GameObject prefabRoot)
        {
            if (prefabRoot == null)
                throw new ArgumentNullException(nameof(prefabRoot));

            SupplierMeetingRuntime.EnsurePrefab(prefabRoot);
            SupplierStashRuntime.EnsurePrefab(prefabRoot);
        }

        /// <summary>
        /// Finalizes identity-dependent prefab resources after ConfigurePrefab has assigned the NPC ID.
        /// </summary>
        internal static bool FinalizePrefabInfrastructure(GameObject prefabRoot, string? explicitId = null)
        {
            if (prefabRoot == null)
                return false;

            string stableId = SupplierRuntimeIds.ResolveStableId(prefabRoot, explicitId);
            SupplierStashRuntime.FinalizePrefab(prefabRoot, stableId);
            return SupplierDeliveryVehicleRuntime.RegisterSupplier(prefabRoot, stableId);
        }

        internal static bool EnsureAllDeliveryPrefabsRegistered()
        {
            return SupplierDeliveryVehicleRuntime.EnsureAllPrefabsRegistered();
        }

        internal static void BindPrefabInfrastructure(S1Economy.Supplier supplier)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            SupplierStashRuntime.BindPrefab(supplier);
            SupplierShopRuntime.ClearDonorReference(supplier);
        }

        internal static bool EnsureReady(
            S1Economy.Supplier supplier,
            string supplierId,
            SupplierDataBuilder.SupplierConfigData? config)
        {
            if (supplier == null)
                return false;

            try
            {
                string stableId = SupplierRuntimeIds.ResolveStableId(
                    supplier.gameObject,
                    config?.PersistentId ?? supplierId);
                SupplierStashRuntime.Bind(
                    supplier,
                    stableId,
                    config?.StashDeadDropGuid);
                S1Shop.ShopInterface shop = SupplierShopRuntime.Ensure(supplier, stableId, config);
                bool vehicleReady = SupplierDeliveryVehicleRuntime.Ensure(supplier, shop, stableId);
                bool meetingReady = SupplierMeetingRuntime.EnsureSelected(supplier);
                bool meetingDialogueReady =
                    SupplierMeetingRuntime.PrepareNativeStartDialogue(supplier);
                SupplierStashRuntime.SchedulePlacement(
                    supplier,
                    config?.StashDeadDropGuid);

                return shop != null
                       && vehicleReady
                       && supplier.Stash?.Storage != null
                       && meetingReady
                       && meetingDialogueReady;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to prepare supplier '{supplierId ?? supplier.name}': {ex.Message}");
                return false;
            }
        }

        internal static bool IsOwnedShop(S1Shop.ShopInterface? shop)
        {
            return SupplierRuntimeIds.IsOwnedShop(shop);
        }

        internal static bool EnsureDeliveryEntry(
            S1Delivery.DeliveryApp app,
            S1Shop.ShopInterface shop,
            bool available)
        {
            return SupplierDeliveryUiBridge.EnsureEntry(app, shop, available);
        }

        internal static bool TryDeferDeliveryStatusDisplay(
            S1Delivery.DeliveryApp app,
            S1DeliveryModel.DeliveryInstance delivery)
        {
            return SupplierDeliveryStatusDisplayRecovery.TryDefer(app, delivery);
        }

        internal static bool TryInitializeGeneratedStash(S1Economy.SupplierStash stash)
        {
            return SupplierStashRuntime.TryInitialize(stash);
        }

        internal static void ReconcileDeliveryUnlock(S1Economy.Supplier supplier)
        {
            if (supplier == null
                || !supplier.IsServerInitialized
                || supplier.RelationData == null
                || supplier.RelationData.RelationDelta < 5f
                || supplier.DeliveriesEnabled)
            {
                return;
            }

            try
            {
                var method = ReflectionUtils.GetMethod(
                    supplier.GetType(),
                    "RelationshipChange",
                    System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance);
                method?.Invoke(supplier, new object[] { 0f });
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to restore delivery availability for supplier '{supplier.ID}': {ex.Message}");
            }
        }

        internal static void ReconcileMeetingDialogue(
            S1Economy.Supplier supplier,
            bool visible)
        {
            if (supplier == null)
                return;

            bool active = SupplierMeetingDialoguePolicy.ShouldActivate(
                visible,
                supplier.Status == S1Economy.Supplier.ESupplierStatus.Meeting);
            if (!SupplierMeetingRuntime.SetDialogueActive(supplier, active)
                && active)
            {
                Logger.Warning(
                    $"Supplier '{supplier.ID}' entered a meeting without a bound shop dialogue choice.");
            }
        }

        internal static void CleanupSupplier(S1Economy.Supplier supplier)
        {
            if (supplier == null)
                return;

            SupplierDeliveryStatusDisplayRecovery.CleanupShop(supplier.Shop?.ShopName);
            SupplierDeliveryUiBridge.CleanupShop(supplier.Shop);
            SupplierDeliveryVehicleRuntime.CleanupSupplier(supplier);
            SupplierShopRuntime.CleanupSupplier(supplier);
            SupplierStashRuntime.CleanupSupplier(supplier);
        }

        internal static void CleanupForSceneChange()
        {
            SupplierDeliveryRecovery.Reset();
            DeliveryEventBridge.Reset();
            SupplierStashPersistenceRuntime.ClearPending();
            SupplierDeliveryStatusDisplayRecovery.Reset();
            SupplierDeliveryUiBridge.CleanupForSceneChange();
            SupplierDeliveryVehicleRuntime.CleanupForSceneChange();
            SupplierShopRuntime.CleanupForSceneChange();
            SupplierStashRuntime.CleanupForSceneChange();
        }
    }
}
