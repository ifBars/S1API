#if (IL2CPPMELON)
using S1Storage = Il2CppScheduleOne.Storage;
using S1EntityFramework = Il2CppScheduleOne.EntityFramework;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1ObjectScripts = Il2CppScheduleOne.ObjectScripts;
using S1Persistence = Il2CppScheduleOne.Persistence.Datas;
using Il2CppInterop.Runtime;
using Il2CppSystem.Collections.Generic;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Storage = ScheduleOne.Storage;
using S1EntityFramework = ScheduleOne.EntityFramework;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1ObjectScripts = ScheduleOne.ObjectScripts;
using S1Persistence = ScheduleOne.Persistence.Datas;
#endif

using HarmonyLib;
using S1API.Storage;
using S1API.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;
#if (IL2CPPMELON)
using S1PersistenceLoaders = Il2CppScheduleOne.Persistence.Loaders;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1PersistenceLoaders = ScheduleOne.Persistence.Loaders;
#endif

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Harmony patches for the storage system.
    /// Intercepts storage creation and loading to raise StorageEvents.
    /// </summary>
    [HarmonyPatch]
    internal static class StoragePatches
    {
        private static readonly Log Logger = new Log("StoragePatches");
        private static readonly HashSet<int> _processedStorages = new HashSet<int>();
        private const string ExtraSlotMetaKey = "S1API_Storage_SlotMeta";

        [Serializable]
        private class StorageSlotMeta : S1Persistence.SaveData
        {
            public int SlotCount;
            public int DisplayRowCount;
            public string ItemId;
        }

        /// <summary>
        /// Patch for PlaceableStorageEntity.Start - raises OnStorageCreated event.
        /// This fires when storage items are placed in the world.
        /// </summary>
        [HarmonyPatch(typeof(S1ObjectScripts.PlaceableStorageEntity), "Start")]
        [HarmonyPostfix]
        private static void PlaceableStorageEntity_Start_Postfix(S1ObjectScripts.PlaceableStorageEntity __instance)
        {
            if (__instance?.ItemInstance?.Definition == null)
                return;

            if (__instance.StorageEntity == null)
                return;

            // Prevent duplicate processing
            int instanceId = __instance.GetInstanceID();
            if (_processedStorages.Contains(instanceId))
                return;
            _processedStorages.Add(instanceId);

            try
            {
                var storageWrapper = new StorageEntity(__instance.StorageEntity, __instance);
                var args = new StorageEventArgs(storageWrapper);
                StorageEvents.RaiseStorageCreated(args);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in PlaceableStorageEntity_Start_Postfix: {ex.Message}");
            }
        }

        /// <summary>
        /// Patch for ItemSet.LoadTo - raises OnStorageLoading event before loading items.
        /// This allows mods to expand storage slots to fit saved items.
        /// </summary>
#if (IL2CPPMELON)
        [HarmonyPatch(typeof(S1Persistence.ItemSet), nameof(S1Persistence.ItemSet.LoadTo), new Type[] { typeof(Il2CppSystem.Collections.Generic.List<S1ItemFramework.ItemSlot>) })]
#else
        [HarmonyPatch(typeof(S1Persistence.ItemSet), nameof(S1Persistence.ItemSet.LoadTo), new Type[] { typeof(System.Collections.Generic.List<S1ItemFramework.ItemSlot>) })]
#endif
        [HarmonyPrefix]
        private static void ItemSet_LoadTo_Prefix(S1Persistence.ItemSet __instance, System.Collections.Generic.List<S1ItemFramework.ItemSlot> slots)
        {
            if (__instance.Items == null || slots == null)
                return;

            if (__instance.Items.Length <= slots.Count)
                return; // No expansion needed

            if (slots.Count == 0)
                return;

            try
            {
                // Get the storage entity from the slot owner
                var owner = slots[0].SlotOwner;
                if (owner == null)
                    return;

                S1Storage.StorageEntity storageEntity = null;

#if (IL2CPPMELON || IL2CPPBEPINEX)
                storageEntity = owner.TryCast<S1Storage.StorageEntity>();
#else
                storageEntity = owner as S1Storage.StorageEntity;
#endif

                if (storageEntity == null)
                    return;

                // Get the placeable storage entity (if this is placeable storage)
                var placeableStorage = storageEntity.GetComponentInParent<S1ObjectScripts.PlaceableStorageEntity>();

                // Wrap and raise event
                var storageWrapper = new StorageEntity(storageEntity, placeableStorage);
                var args = new StorageLoadingEventArgs(storageWrapper, __instance.Items.Length);
                StorageEvents.RaiseStorageLoading(args);

                // Sync the provided slot list with any new slots added by handlers so LoadTo can populate them
                int actualSlotCount = storageEntity.ItemSlots.Count;
                if (slots.Count < actualSlotCount)
                {
                    for (int i = slots.Count; i < actualSlotCount; i++)
                    {
                        slots.Add(storageEntity.ItemSlots[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in ItemSet_LoadTo_Prefix: {ex.Message}");
            }
        }

        /// <summary>
        /// Persist slot topology for placeable storage so expanded slots reload correctly.
        /// Patch at BuildableItem level to cover IL2CPP/Mono generated subclasses.
        /// </summary>
        [HarmonyPatch(typeof(S1EntityFramework.BuildableItem), nameof(S1EntityFramework.BuildableItem.GetSaveData))]
        [HarmonyPostfix]
        private static void BuildableItem_GetSaveData_Postfix(S1EntityFramework.BuildableItem __instance, ref S1Persistence.DynamicSaveData __result)
        {
            var placeable = __instance as S1ObjectScripts.PlaceableStorageEntity;
            if (__result == null || placeable?.StorageEntity == null)
                return;

            try
            {
                var meta = new StorageSlotMeta
                {
                    SlotCount = placeable.StorageEntity.ItemSlots != null ? placeable.StorageEntity.ItemSlots.Count : placeable.StorageEntity.SlotCount,
                    DisplayRowCount = placeable.StorageEntity.DisplayRowCount,
                    ItemId = placeable.ItemInstance?.Definition?.ID
                };

                __result.AddData(ExtraSlotMetaKey, meta);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error adding storage slot metadata: {ex.Message}");
            }
        }

        /// <summary>
        /// Hydrate expanded slots before loading contents so extra slots are restored from saves.
        /// </summary>
        [HarmonyPatch(typeof(S1PersistenceLoaders.StorageRackLoader), "Load", new Type[] { typeof(S1Persistence.DynamicSaveData) })]
        [HarmonyPrefix]
        private static bool StorageRackLoader_Load_Prefix(S1PersistenceLoaders.StorageRackLoader __instance, S1Persistence.DynamicSaveData data)
        {
            try
            {
                if (data == null)
                    return false;

                S1Persistence.GridItemData gridItemData;
                if (!data.TryExtractBaseData<S1Persistence.GridItemData>(out gridItemData) || gridItemData == null)
                    return false;

                // Use reflection to call protected LoadAndCreate(GridItemData data)
                var loadAndCreate = AccessTools.Method(__instance.GetType(), "LoadAndCreate", new Type[] { typeof(S1Persistence.GridItemData) });
                var gridItem = loadAndCreate?.Invoke(__instance, new object[] { gridItemData }) as S1EntityFramework.GridItem;
                if (gridItem == null)
                    return false;

                var placeableStorage = gridItem as S1ObjectScripts.PlaceableStorageEntity;
                if (placeableStorage == null)
                    return false;

                S1Persistence.PlaceableStorageData storageData;
                if (!data.TryExtractBaseData<S1Persistence.PlaceableStorageData>(out storageData) || storageData == null)
                    return false;

                int targetSlots = storageData.Contents?.Items?.Length ?? placeableStorage.StorageEntity.ItemSlots.Count;

                // Use non-generic TryGetData to avoid IL2CPP reflection issues
                if (data.TryGetData(ExtraSlotMetaKey, out string metaJson) && !string.IsNullOrEmpty(metaJson))
                {
                    try
                    {
                        var meta = JsonUtility.FromJson<StorageSlotMeta>(metaJson);
                        if (meta != null)
                        {
                            targetSlots = Math.Max(targetSlots, meta.SlotCount);
                            if (meta.DisplayRowCount > placeableStorage.StorageEntity.DisplayRowCount)
                            {
                                placeableStorage.StorageEntity.DisplayRowCount = meta.DisplayRowCount;
                            }
                        }
                    }
                    catch (Exception metaEx)
                    {
                        Logger.Warning($"Failed to deserialize storage slot metadata: {metaEx.Message}");
                    }
                }

                // Expand slots before hydrating contents
                var wrapper = new StorageEntity(placeableStorage.StorageEntity, placeableStorage);
                wrapper.SetSlotCount(targetSlots);

                storageData.Contents.LoadTo(placeableStorage.StorageEntity.ItemSlots);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in StorageRackLoader_Load_Prefix: {ex.Message}");
            }

            // Skip original loader to avoid double-loading
            return false;
        }
    }
}
