#if IL2CPPMELON
using Il2CppInterop.Runtime;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Economy = Il2CppScheduleOne.Economy;
using S1GuidRegisterable = Il2CppScheduleOne.IGUIDRegisterable;
using S1Items = Il2CppScheduleOne.ItemFramework;
using S1Relation = Il2CppScheduleOne.NPCs.Relation;
using S1Storage = Il2CppScheduleOne.Storage;
#elif MONOMELON
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Economy = ScheduleOne.Economy;
using S1GuidRegisterable = ScheduleOne.IGUIDRegisterable;
using S1Items = ScheduleOne.ItemFramework;
using S1Relation = ScheduleOne.NPCs.Relation;
using S1Storage = ScheduleOne.Storage;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MelonLoader;
using S1API.DeadDrops;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace S1API.Internal.Entities.Suppliers
{
    /// <summary>
    /// Creates, initializes, places, and cleans up supplier stash objects.
    /// </summary>
    internal static class SupplierStashRuntime
    {
        internal const string StashObjectName = "S1API_SupplierStash";

        private static readonly Log Logger = new Log("SupplierStashRuntime");
        private static readonly Dictionary<int, GameObject> OwnedStashes = new Dictionary<int, GameObject>();
        private static readonly HashSet<int> InitializedStashes = new HashSet<int>();
        private static readonly HashSet<int> ScheduledPlacements = new HashSet<int>();
        private static readonly HashSet<int> ConfiguredStashes = new HashSet<int>();
        private static readonly Dictionary<int, string> ConfiguredStashGuids =
            new Dictionary<int, string>();

        internal static void EnsurePrefab(GameObject prefabRoot)
        {
            S1Economy.SupplierStash? existing = prefabRoot
                .GetComponentsInChildren<S1Economy.SupplierStash>(true)
                .FirstOrDefault(candidate => candidate != null && candidate.gameObject.name == StashObjectName);
            if (existing != null)
                return;

            var stashObject = new GameObject(StashObjectName);
            stashObject.SetActive(false);
            stashObject.transform.SetParent(prefabRoot.transform, false);
            stashObject.transform.localPosition = new Vector3(1.5f, 0.5f, 0f);

            var storage = stashObject.AddComponent<S1Storage.WorldStorageEntity>();
            storage.SlotCount = 5;
            storage.DisplayRowCount = 1;
            storage.AccessSettings = S1Storage.StorageEntity.EAccessSettings.Full;
            storage.MaxAccessDistance = 5f;

            var collider = stashObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.1f, 0.8f, 0.8f);
            collider.isTrigger = true;

            var interactable = stashObject.AddComponent<S1Storage.StorageEntityInteractable>();
            interactable.MaxInteractionRange = storage.MaxAccessDistance;

            var lightObject = new GameObject("IndicatorLight");
            lightObject.transform.SetParent(stashObject.transform, false);
            var unityLight = lightObject.AddComponent<Light>();
            unityLight.range = 2f;
            unityLight.intensity = 1f;
            var optimizedLight = lightObject.AddComponent<S1DevUtilities.OptimizedLight>();
            optimizedLight.Enabled = false;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "StashVisual";
            visual.transform.SetParent(stashObject.transform, false);
            visual.transform.localScale = new Vector3(1f, 0.7f, 0.7f);
            Collider? visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
                Object.DestroyImmediate(visualCollider);

            var stash = stashObject.AddComponent<S1Economy.SupplierStash>();
            stash.locationDescription = "beside the supplier's starting location";
            stash.Storage = storage;
            stash.IntObj = interactable;
            stash.Light = optimizedLight;
            stash.StashPoI = null;
            stashObject.SetActive(true);
        }

        internal static void FinalizePrefab(GameObject prefabRoot, string stableId)
        {
            SupplierStashPersistenceRuntime.Register(stableId);

            S1Economy.SupplierStash? stash = FindInHierarchy(prefabRoot);
            if (stash?.Storage != null
                && CrossType.Is(stash.Storage, out S1Storage.WorldStorageEntity worldStorage))
            {
                ConfigureIdentity(worldStorage, stableId);
            }
        }

        internal static void Bind(
            S1Economy.Supplier supplier,
            string stableId,
            string? stashDeadDropGuid)
        {
            if (!string.IsNullOrWhiteSpace(stashDeadDropGuid))
            {
                BindDeadDrop(supplier, stashDeadDropGuid);
                return;
            }

            S1Economy.SupplierStash stash = BindPrefab(supplier);

            if (stash.Storage != null
                && CrossType.Is(stash.Storage, out S1Storage.WorldStorageEntity worldStorage))
            {
                ConfigureIdentity(worldStorage, stableId);
                SupplierStashPersistenceRuntime.Register(stableId);
            }
        }

        internal static S1Economy.SupplierStash BindPrefab(S1Economy.Supplier supplier)
        {
            S1Economy.SupplierStash? stash = Find(supplier);
            if (stash == null && !supplier.IsSpawned)
            {
                EnsurePrefab(supplier.gameObject);
                stash = Find(supplier);
            }

            if (stash == null)
                throw new InvalidOperationException("The generated supplier prefab has no S1API stash.");

            stash.Supplier = supplier;
            supplier.Stash = stash;
            return stash;
        }

        internal static bool TryInitialize(S1Economy.SupplierStash stash)
        {
            if (stash == null || stash.gameObject == null)
                return false;

            bool configuredStash = ConfiguredStashes.Contains(stash.GetInstanceID());
            if (!configuredStash && stash.gameObject.name != StashObjectName)
                return false;

            int instanceId = stash.GetInstanceID();
            if (!InitializedStashes.Add(instanceId))
                return true;

            if (stash.Supplier == null || stash.Storage == null || stash.IntObj == null)
            {
                Logger.Error("Generated supplier stash is missing Supplier, Storage, or Interactable references.");
                return true;
            }

            if (!configuredStash &&
                CrossType.Is(stash.Storage, out S1Storage.WorldStorageEntity worldStorage))
            {
                string stableId = SupplierRuntimeIds.ResolveStableId(
                    stash.Supplier.gameObject,
                    stash.Supplier.ID);
                SupplierStashPersistenceRuntime.TryReplay(worldStorage, stableId);
            }

            stash.IntObj.SetMessage($"View {stash.Supplier.FullName}'s stash");
            stash.IntObj.enabled = stash.Supplier.RelationData.Unlocked;
            stash.IntObj.onInteractStart.AddListener((UnityAction)(() => UpdateSubtitle(stash)));
            stash.Storage.StorageEntityName = $"{stash.Supplier.FullName}'s Stash";
            if (stash.StashPoI != null)
            {
                stash.StashPoI.enabled = stash.Supplier.RelationData.Unlocked;
                stash.StashPoI.SetMainText($"{stash.Supplier.FullName}'s Stash");
            }
#if IL2CPPMELON
            var onUnlocked = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<S1Relation.NPCRelationData.EUnlockType, bool>>(
                new Action<S1Relation.NPCRelationData.EUnlockType, bool>((_, _) => EnableUnlockedStash(stash)));
            stash.Supplier.RelationData.OnUnlocked = Il2CppSystem.Delegate
                .Combine(stash.Supplier.RelationData.OnUnlocked, onUnlocked)
                .Cast<Il2CppSystem.Action<S1Relation.NPCRelationData.EUnlockType, bool>>();

            var onContentsChanged = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(
                new Action(() => Recalculate(stash)));
            stash.Storage.onContentsChanged = Il2CppSystem.Delegate
                .Combine(stash.Storage.onContentsChanged, onContentsChanged)
                .Cast<Il2CppSystem.Action>();
#else
            stash.Supplier.RelationData.OnUnlocked += (_, _) => EnableUnlockedStash(stash);
            stash.Storage.onContentsChanged += () => Recalculate(stash);
#endif
            Recalculate(stash);
            return true;
        }

        private static void EnableUnlockedStash(S1Economy.SupplierStash stash)
        {
            stash.IntObj.enabled = true;
            if (stash.StashPoI != null)
                stash.StashPoI.enabled = true;
        }

        internal static void SchedulePlacement(
            S1Economy.Supplier supplier,
            string? stashDeadDropGuid)
        {
            if (!string.IsNullOrWhiteSpace(stashDeadDropGuid))
                return;

            int supplierKey = supplier.GetInstanceID();
            if (!ScheduledPlacements.Add(supplierKey))
                return;

            MelonCoroutines.Start(PlaceAfterSpawn(supplier, supplierKey));
        }

        internal static S1Economy.SupplierStash? Find(S1Economy.Supplier supplier)
        {
            if (supplier.Stash != null &&
                (supplier.Stash.gameObject.name == StashObjectName ||
                 ConfiguredStashes.Contains(supplier.Stash.GetInstanceID())))
                return supplier.Stash;

            if (OwnedStashes.TryGetValue(supplier.GetInstanceID(), out GameObject? ownedStash)
                && ownedStash != null)
            {
                return ownedStash.GetComponent<S1Economy.SupplierStash>();
            }

            return FindInHierarchy(supplier.gameObject);
        }

        internal static void CleanupSupplier(S1Economy.Supplier supplier)
        {
            if (supplier == null)
                return;

            int supplierKey = supplier.GetInstanceID();
            if (ConfiguredStashGuids.Remove(supplierKey, out string? configuredGuid) &&
                supplier.Stash != null)
            {
                ConfiguredStashes.Remove(supplier.Stash.GetInstanceID());
            }
            if (OwnedStashes.TryGetValue(supplierKey, out GameObject? stashObject))
            {
                Destroy(stashObject);
                OwnedStashes.Remove(supplierKey);
            }

            ScheduledPlacements.Remove(supplierKey);
        }

        internal static void CleanupForSceneChange()
        {
            foreach (GameObject stash in OwnedStashes.Values.ToArray())
                Destroy(stash);

            OwnedStashes.Clear();
            InitializedStashes.Clear();
            ScheduledPlacements.Clear();
            ConfiguredStashes.Clear();
            ConfiguredStashGuids.Clear();
        }

        private static IEnumerator PlaceAfterSpawn(S1Economy.Supplier supplier, int supplierKey)
        {
            float deadline = Time.realtimeSinceStartup + 30f;
            while (supplier != null && !supplier.IsSpawned && Time.realtimeSinceStartup < deadline)
                yield return null;

            // FinalizeNetworkSpawn applies the configured NPC transform immediately after FishNet spawn.
            // Waiting one frame lets the stash inherit that transform before becoming a world object.
            yield return null;

            try
            {
                if (supplier == null || !supplier.IsSpawned)
                {
                    Logger.Error("A custom supplier stash could not be placed because its NPC never spawned.");
                    yield break;
                }

                S1Economy.SupplierStash? stash = Find(supplier);
                if (stash == null)
                {
                    Logger.Error($"Custom supplier '{supplier.ID}' lost its generated stash after network spawn.");
                    yield break;
                }

                stash.Supplier = supplier;
                supplier.Stash = stash;
                if (stash.transform.parent != null)
                    stash.transform.SetParent(null, true);

                stash.gameObject.name = StashObjectName;
                OwnedStashes[supplierKey] = stash.gameObject;
                SupplierRuntimeCoordinator.ReconcileDeliveryUnlock(supplier);
            }
            finally
            {
                ScheduledPlacements.Remove(supplierKey);
            }
        }

        private static S1Economy.SupplierStash? FindInHierarchy(GameObject root)
        {
            return root
                .GetComponentsInChildren<S1Economy.SupplierStash>(true)
                .FirstOrDefault(candidate => candidate != null && candidate.gameObject.name == StashObjectName);
        }

        private static void BindDeadDrop(
            S1Economy.Supplier supplier,
            string deadDropGuid)
        {
            DeadDropInstance deadDrop =
                DeadDropManager.GetByGUID(deadDropGuid) ??
                throw new InvalidOperationException(
                    $"Supplier stash dead drop '{deadDropGuid}' is not present in the scene.");
            S1Economy.DeadDrop nativeDeadDrop = deadDrop.S1DeadDrop;
            int supplierKey = supplier.GetInstanceID();

            foreach (KeyValuePair<int, string> reservation in ConfiguredStashGuids)
            {
                if (reservation.Key != supplierKey &&
                    string.Equals(
                        reservation.Value,
                        deadDrop.GUID,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Dead drop '{deadDrop.Name}' is already assigned to another supplier stash.");
                }
            }

            S1Storage.StorageEntityInteractable interactable =
                nativeDeadDrop.Storage.GetComponent<S1Storage.StorageEntityInteractable>() ??
                nativeDeadDrop.Storage.GetComponentInChildren<S1Storage.StorageEntityInteractable>(true) ??
                throw new InvalidOperationException(
                    $"Dead drop '{deadDrop.Name}' has no storage interaction component.");

            RemoveGeneratedPrefabStash(supplier);

            S1Economy.SupplierStash stash =
                nativeDeadDrop.GetComponent<S1Economy.SupplierStash>() ??
                nativeDeadDrop.gameObject.AddComponent<S1Economy.SupplierStash>();

            stash.Supplier = supplier;
            stash.Storage = nativeDeadDrop.Storage;
            stash.IntObj = interactable;
            stash.Light = nativeDeadDrop.Light;
            stash.StashPoI = nativeDeadDrop.PoI;
            stash.locationDescription = nativeDeadDrop.DeadDropDescription;
            supplier.Stash = stash;

            ConfiguredStashes.Add(stash.GetInstanceID());
            ConfiguredStashGuids[supplierKey] = deadDrop.GUID;
        }

        private static void RemoveGeneratedPrefabStash(S1Economy.Supplier supplier)
        {
            int supplierKey = supplier.GetInstanceID();
            GameObject? generatedStashObject = null;
            if (OwnedStashes.TryGetValue(supplierKey, out GameObject? ownedStash))
            {
                generatedStashObject = ownedStash;
                OwnedStashes.Remove(supplierKey);
            }

            S1Economy.SupplierStash? generatedStash =
                generatedStashObject?.GetComponent<S1Economy.SupplierStash>() ??
                FindInHierarchy(supplier.gameObject);
            generatedStashObject ??= generatedStash?.gameObject;
            if (generatedStashObject == null)
                return;

            if (generatedStash?.IntObj != null)
                generatedStash.IntObj.enabled = false;

            generatedStashObject.SetActive(false);
            Destroy(generatedStashObject);
        }

        internal static bool IsReservedDeadDrop(S1Economy.DeadDrop deadDrop)
        {
            if (deadDrop == null)
                return false;

            string guid = deadDrop.GUID.ToString("D");
            return ConfiguredStashGuids.Values.Any(value =>
                string.Equals(value, guid, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool HasReservedDeadDrops =>
            ConfiguredStashGuids.Count > 0;

        private static void ConfigureIdentity(S1Storage.WorldStorageEntity storage, string stableId)
        {
            string bakedGuid = SupplierRuntimeIds.GetStashGuid(stableId).ToString();
            if (!ReflectionUtils.TrySetFieldOrProperty(storage, "BakedGUID", bakedGuid))
            {
                throw new InvalidOperationException(
                    $"Could not assign a persistent GUID to supplier stash '{stableId}'.");
            }
        }

        private static void Destroy(GameObject stashObject)
        {
            if (stashObject == null)
                return;

            S1Economy.SupplierStash? stash = stashObject.GetComponent<S1Economy.SupplierStash>();
            if (stash != null)
                InitializedStashes.Remove(stash.GetInstanceID());

            S1Storage.WorldStorageEntity? storage = stashObject.GetComponent<S1Storage.WorldStorageEntity>();
            if (storage != null)
            {
                try
                {
#if IL2CPPMELON
                    if (CrossType.Is(storage, out S1GuidRegisterable guidStorage))
                        Il2Cpp.GUIDManager.DeregisterObject(guidStorage);
#else
                    if (CrossType.Is(storage, out S1GuidRegisterable guidStorage))
                        global::GUIDManager.DeregisterObject(guidStorage);
#endif
                }
                catch
                {
                }
            }

            Object.Destroy(stashObject);
        }

        private static void UpdateSubtitle(S1Economy.SupplierStash stash)
        {
            stash.Storage.StorageEntitySubtitle =
                $"You owe {stash.Supplier.FullName} ${stash.Supplier.Debt:0.00}. Insert cash and exit the stash to repay the debt.";
        }

        private static void Recalculate(S1Economy.SupplierStash stash)
        {
            float cash = 0f;
            for (int i = 0; i < stash.Storage.ItemSlots.Count; i++)
            {
                var instance = stash.Storage.ItemSlots[i]?.ItemInstance;
                if (instance != null && CrossType.Is(instance, out S1Items.CashInstance cashInstance))
                    cash += cashInstance.Balance;
            }

#if IL2CPPMELON
            stash._CashAmount_k__BackingField = cash;
#else
            ReflectionUtils.TrySetFieldOrProperty(stash, "CashAmount", cash);
#endif
            if (stash.Light != null)
                stash.Light.Enabled = stash.Storage.ItemCount > 0;
            UpdateSubtitle(stash);
        }
    }
}
