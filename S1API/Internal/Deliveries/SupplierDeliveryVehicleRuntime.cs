#if IL2CPPMELON
using FishNetInstanceFinder = Il2CppFishNet.InstanceFinder;
using FishNetNetworkObject = Il2CppFishNet.Object.NetworkObject;
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Economy = Il2CppScheduleOne.Economy;
using S1GuidRegisterable = Il2CppScheduleOne.IGUIDRegisterable;
using S1Shop = Il2CppScheduleOne.UI.Shop;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
#elif MONOMELON
using FishNetInstanceFinder = FishNet.InstanceFinder;
using FishNetNetworkObject = FishNet.Object.NetworkObject;
using S1Delivery = ScheduleOne.Delivery;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Economy = ScheduleOne.Economy;
using S1GuidRegisterable = ScheduleOne.IGUIDRegisterable;
using S1Shop = ScheduleOne.UI.Shop;
using S1Vehicles = ScheduleOne.Vehicles;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using S1API.Entities;
using S1API.Internal.Entities.Suppliers;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Internal.Deliveries
{
    /// <summary>
    /// Registers, spawns, binds, and cleans up one private networked delivery van per custom supplier.
    /// </summary>
    internal static class SupplierDeliveryVehicleRuntime
    {
        private const string VehicleCode = "veeper";

        private static readonly Log Logger = new Log("SupplierDeliveryVehicleRuntime");
        private static readonly HashSet<string> RegisteredSupplierIds =
            new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, FishNetNetworkObject> Prefabs =
            new Dictionary<string, FishNetNetworkObject>(StringComparer.Ordinal);
        private static readonly Dictionary<int, GameObject> OwnedVehicles =
            new Dictionary<int, GameObject>();
        private static readonly HashSet<int> ScheduledBindings = new HashSet<int>();

        internal static bool PrefabsReadyForLocalProcess
        {
            get
            {
                if (RegisteredSupplierIds.Count == 0)
                    return true;

                var spawnables = FishNetInstanceFinder.NetworkManager?.SpawnablePrefabs;
                if (spawnables == null)
                    return false;

                foreach (string stableId in RegisteredSupplierIds)
                {
            if (!Prefabs.TryGetValue(stableId, out FishNetNetworkObject? prefab)
                        || prefab == null
                        || !IsRegistered(prefab))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        internal static bool RegisterSupplier(GameObject prefabRoot, string? explicitId = null)
        {
            string stableId = SupplierRuntimeIds.ResolveStableId(prefabRoot, explicitId);
            RegisteredSupplierIds.Add(stableId);
            return EnsurePrefab(stableId);
        }

        internal static bool EnsureAllPrefabsRegistered()
        {
            bool allReady = true;
            foreach (string stableId in RegisteredSupplierIds.OrderBy(value => value, StringComparer.Ordinal))
                allReady &= EnsurePrefab(stableId);
            return allReady;
        }

        internal static bool Ensure(
            S1Economy.Supplier supplier,
            S1Shop.ShopInterface shop,
            string stableId)
        {
            RegisteredSupplierIds.Add(stableId);
            if (TryBind(supplier, shop, stableId))
                return true;

            shop.DeliveryVehicle = null;
            if (!EnsurePrefab(stableId))
            {
                Logger.Error(
                    $"Custom supplier '{supplier.ID}' cannot start because its delivery vehicle prefab is unavailable.");
                return false;
            }

            if (!FishNetInstanceFinder.IsServer)
            {
                ScheduleBinding(supplier, shop, stableId);
                return true;
            }

            try
            {
                FishNetNetworkObject prefab = Prefabs[stableId];
                GameObject instanceObject = Object.Instantiate(prefab.gameObject);
                instanceObject.name = prefab.gameObject.name + "_Runtime";
                instanceObject.transform.SetParent(null, false);
                instanceObject.transform.position = new Vector3(0f, -100f, 0f);
                instanceObject.transform.rotation = Quaternion.identity;
                instanceObject.SetActive(true);

                FishNetNetworkObject instance = instanceObject.GetComponent<FishNetNetworkObject>()
                    ?? throw new InvalidOperationException("Spawned delivery vehicle has no NetworkObject.");
                S1Delivery.DeliveryVehicle deliveryVehicle =
                    instanceObject.GetComponent<S1Delivery.DeliveryVehicle>()
                    ?? throw new InvalidOperationException("Spawned delivery vehicle has no DeliveryVehicle component.");

                FishNetInstanceFinder.ServerManager.Spawn(
                    instance,
                    null,
                    default(UnityEngine.SceneManagement.Scene));

                Bind(supplier, shop, deliveryVehicle);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to spawn the delivery vehicle for supplier '{supplier.ID}': {ex.Message}");
                return false;
            }
        }

        internal static bool IsKnownGuid(string? guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                return false;

            foreach (string stableId in RegisteredSupplierIds)
            {
                if (string.Equals(
                    SupplierRuntimeIds.GetDeliveryVehicleGuid(stableId).ToString(),
                    guid,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static S1Delivery.DeliveryVehicle? FindByGuid(string guid)
        {
            return Resources
                .FindObjectsOfTypeAll<S1Delivery.DeliveryVehicle>()
                .FirstOrDefault(candidate => candidate != null
                                             && string.Equals(
                                                 candidate.GUID,
                                                 guid,
                                                 StringComparison.OrdinalIgnoreCase));
        }

        internal static void CleanupSupplier(S1Economy.Supplier supplier)
        {
            if (supplier == null)
                return;

            int supplierKey = supplier.GetInstanceID();
            ScheduledBindings.Remove(supplierKey);
            if (!OwnedVehicles.TryGetValue(supplierKey, out GameObject? vehicleObject))
                return;

            DestroyVehicle(vehicleObject);
            OwnedVehicles.Remove(supplierKey);
        }

        internal static void CleanupForSceneChange()
        {
            foreach (GameObject vehicle in OwnedVehicles.Values.ToArray())
                DestroyVehicle(vehicle);

            OwnedVehicles.Clear();
            ScheduledBindings.Clear();
        }

        private static bool EnsurePrefab(string stableId)
        {
            if (Prefabs.TryGetValue(stableId, out FishNetNetworkObject? existing) && existing != null)
                return EnsureRegistered(existing);

            var networkManager = FishNetInstanceFinder.NetworkManager;
            if (networkManager?.SpawnablePrefabs == null)
                return false;

            S1Vehicles.VehicleManager? manager = null;
            try
            {
                manager = S1DevUtilities.NetworkSingleton<S1Vehicles.VehicleManager>.Instance;
            }
            catch
            {
            }

            S1Vehicles.LandVehicle? source = manager?.GetVehiclePrefab(VehicleCode);
            if (source == null || source.gameObject == null)
                return false;

            GameObject? clone = null;
            try
            {
                clone = InactiveObjectCloner.CloneGameObject(source.gameObject);
                clone.name = SupplierRuntimeIds.GetDeliveryVehiclePrefabName(stableId);
                clone.transform.position = new Vector3(0f, -100f, 0f);
                clone.transform.rotation = Quaternion.identity;

                S1Delivery.DeliveryVehicle deliveryVehicle =
                    clone.GetComponent<S1Delivery.DeliveryVehicle>()
                    ?? clone.AddComponent<S1Delivery.DeliveryVehicle>();
                deliveryVehicle.GUID = SupplierRuntimeIds.GetDeliveryVehicleGuid(stableId).ToString();

                FishNetNetworkObject networkObject = clone.GetComponent<FishNetNetworkObject>()
                    ?? throw new InvalidOperationException(
                        $"Vehicle prefab '{source.VehicleCode}' has no FishNet NetworkObject.");

                NPCPrefabContainer.OrganizePrefab(
                    clone,
                    $"{SupplierRuntimeIds.Sanitize(stableId)}_SupplierDeliveryVehicle");
                if (!EnsureRegistered(networkObject))
                    throw new InvalidOperationException("FishNet rejected the custom delivery vehicle prefab.");

                Prefabs[stableId] = networkObject;
                return true;
            }
            catch (Exception ex)
            {
                if (clone != null)
                    Object.DestroyImmediate(clone);
                Logger.Warning(
                    $"Could not prepare the delivery vehicle prefab for supplier '{stableId}': {ex.Message}");
                return false;
            }
        }

        private static bool EnsureRegistered(FishNetNetworkObject networkObject)
        {
            if (networkObject == null || networkObject.gameObject == null)
                return false;

            var spawnables = FishNetInstanceFinder.NetworkManager?.SpawnablePrefabs;
            if (spawnables == null)
                return false;
            if (IsRegistered(networkObject))
                return true;

            global::S1API.Entities.NPC.RegisterSpawnablePrefabInOrder(spawnables, networkObject);
            return true;
        }

        private static bool IsRegistered(FishNetNetworkObject networkObject)
        {
            var spawnables = FishNetInstanceFinder.NetworkManager?.SpawnablePrefabs;
            if (spawnables == null)
                return false;

            int count = spawnables.GetObjectCount();
            for (int i = 0; i < count; i++)
            {
                FishNetNetworkObject candidate = spawnables.GetObject(true, i);
                if (candidate == networkObject
                    || (candidate?.gameObject != null
                        && string.Equals(
                            candidate.gameObject.name,
                            networkObject.gameObject.name,
                            StringComparison.Ordinal)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryBind(
            S1Economy.Supplier supplier,
            S1Shop.ShopInterface shop,
            string stableId)
        {
            string expectedGuid = SupplierRuntimeIds.GetDeliveryVehicleGuid(stableId).ToString();
            if (shop.DeliveryVehicle != null
                && IsSpawnedMatch(shop.DeliveryVehicle, expectedGuid))
            {
                Bind(supplier, shop, shop.DeliveryVehicle);
                return true;
            }

            S1Delivery.DeliveryVehicle? deliveryVehicle = Resources
                .FindObjectsOfTypeAll<S1Delivery.DeliveryVehicle>()
                .FirstOrDefault(candidate => IsSpawnedMatch(candidate, expectedGuid));
            if (deliveryVehicle == null)
                return false;

            Bind(supplier, shop, deliveryVehicle);
            return true;
        }

        private static bool IsSpawnedMatch(S1Delivery.DeliveryVehicle? deliveryVehicle, string expectedGuid)
        {
            if (deliveryVehicle == null
                || !string.Equals(deliveryVehicle.GUID, expectedGuid, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            FishNetNetworkObject? networkObject = deliveryVehicle.GetComponent<FishNetNetworkObject>();
            return networkObject != null && networkObject.IsSpawned;
        }

        private static void Bind(
            S1Economy.Supplier supplier,
            S1Shop.ShopInterface shop,
            S1Delivery.DeliveryVehicle deliveryVehicle)
        {
            shop.DeliveryVehicle = deliveryVehicle;
            OwnedVehicles[supplier.GetInstanceID()] = deliveryVehicle.gameObject;
            SupplierDeliveryRecovery.OnVehicleBound(shop, deliveryVehicle);
            SupplierDeliveryUiBridge.OnVehicleBound(shop);
        }

        private static void ScheduleBinding(
            S1Economy.Supplier supplier,
            S1Shop.ShopInterface shop,
            string stableId)
        {
            int supplierKey = supplier.GetInstanceID();
            if (!ScheduledBindings.Add(supplierKey))
                return;

            MelonCoroutines.Start(BindWhenSpawned(supplier, shop, stableId, supplierKey));
        }

        private static IEnumerator BindWhenSpawned(
            S1Economy.Supplier supplier,
            S1Shop.ShopInterface shop,
            string stableId,
            int supplierKey)
        {
            float deadline = Time.realtimeSinceStartup + 30f;
            try
            {
                while (supplier != null && shop != null && Time.realtimeSinceStartup < deadline)
                {
                    if (TryBind(supplier, shop, stableId))
                        yield break;
                    yield return null;
                }

                if (supplier != null)
                    Logger.Error($"Timed out waiting for the delivery vehicle for supplier '{supplier.ID}'.");
            }
            finally
            {
                ScheduledBindings.Remove(supplierKey);
            }
        }

        private static void DestroyVehicle(GameObject vehicleObject)
        {
            if (vehicleObject == null)
                return;

            FishNetNetworkObject? networkObject = vehicleObject.GetComponent<FishNetNetworkObject>();
            if (networkObject != null && networkObject.IsSpawned && !FishNetInstanceFinder.IsServer)
                return;

            S1Delivery.DeliveryVehicle? deliveryVehicle =
                vehicleObject.GetComponent<S1Delivery.DeliveryVehicle>();
            try
            {
                deliveryVehicle?.Deactivate();
            }
            catch
            {
            }

            S1Vehicles.LandVehicle? landVehicle = deliveryVehicle?.Vehicle
                                                      ?? vehicleObject.GetComponent<S1Vehicles.LandVehicle>();
            if (landVehicle != null)
            {
                try
                {
#if IL2CPPMELON
                    if (CrossType.Is(landVehicle, out S1GuidRegisterable guidVehicle))
                        Il2Cpp.GUIDManager.DeregisterObject(guidVehicle);
#else
                    if (CrossType.Is(landVehicle, out S1GuidRegisterable guidVehicle))
                        global::GUIDManager.DeregisterObject(guidVehicle);
#endif
                }
                catch
                {
                }
            }

            if (networkObject != null && networkObject.IsSpawned)
            {
                try
                {
                    // Supplier destruction can run after FishNet has begun shutting down. In that
                    // state IsSpawned may still be true even though Despawn no longer has a manager.
                    if (FishNetInstanceFinder.ServerManager?.Started == true)
                    {
                        networkObject.Despawn();
                        return;
                    }
                }
                catch
                {
                    // Unity destruction below is the safe teardown fallback once FishNet is unavailable.
                }
            }

            Object.Destroy(vehicleObject);
        }
    }
}
