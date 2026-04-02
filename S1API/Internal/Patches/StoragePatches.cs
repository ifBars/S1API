#if (IL2CPPMELON)
using S1Storage = Il2CppScheduleOne.Storage;
using S1EntityFramework = Il2CppScheduleOne.EntityFramework;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1ObjectScripts = Il2CppScheduleOne.ObjectScripts;
using S1Persistence = Il2CppScheduleOne.Persistence.Datas;
using S1UI = Il2CppScheduleOne.UI;
using Il2CppInterop.Runtime;
using Il2CppSystem.Collections.Generic;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Storage = ScheduleOne.Storage;
using S1EntityFramework = ScheduleOne.EntityFramework;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1ObjectScripts = ScheduleOne.ObjectScripts;
using S1Persistence = ScheduleOne.Persistence.Datas;
using S1UI = ScheduleOne.UI;
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
        /// Manually parse the custom name from RenamableConfigurationData JSON.
        /// Handles both compact and pretty-printed formats.
        /// </summary>
        private static string ParseConfigurationName(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                int valueKeyIdx = json.IndexOf("\"Value\"");
                if (valueKeyIdx < 0)
                    return null;

                int colonIdx = json.IndexOf(':', valueKeyIdx + 7);
                if (colonIdx < 0)
                    return null;

                int openQuote = json.IndexOf('"', colonIdx + 1);
                if (openQuote < 0)
                    return null;

                int closeQuote = json.IndexOf('"', openQuote + 1);
                if (closeQuote < 0)
                    return null;

                return json.Substring(openQuote + 1, closeQuote - openQuote - 1);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Manually parse StorageSlotMeta from JSON to avoid IL2CPP generic method issues.
        /// </summary>
        private static StorageSlotMeta ParseStorageSlotMeta(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                // Simple manual parsing since the JSON structure is known and simple
                // Format: {"SlotCount":X,"DisplayRowCount":Y,"ItemId":"Z"}
                var meta = new StorageSlotMeta();

                // Extract SlotCount
                int slotCountIdx = json.IndexOf("\"SlotCount\":");
                if (slotCountIdx >= 0)
                {
                    int valueStart = slotCountIdx + 12;
                    int valueEnd = json.IndexOfAny(new char[] { ',', '}' }, valueStart);
                    if (valueEnd > valueStart && int.TryParse(json.Substring(valueStart, valueEnd - valueStart), out int slotCount))
                    {
                        meta.SlotCount = slotCount;
                    }
                }

                // Extract DisplayRowCount
                int displayRowIdx = json.IndexOf("\"DisplayRowCount\":");
                if (displayRowIdx >= 0)
                {
                    int valueStart = displayRowIdx + 18;
                    int valueEnd = json.IndexOfAny(new char[] { ',', '}' }, valueStart);
                    if (valueEnd > valueStart && int.TryParse(json.Substring(valueStart, valueEnd - valueStart), out int displayRowCount))
                    {
                        meta.DisplayRowCount = displayRowCount;
                    }
                }

                // Extract ItemId
                int itemIdIdx = json.IndexOf("\"ItemId\":\"");
                if (itemIdIdx >= 0)
                {
                    int valueStart = itemIdIdx + 10;
                    int valueEnd = json.IndexOf("\"", valueStart);
                    if (valueEnd > valueStart)
                    {
                        meta.ItemId = json.Substring(valueStart, valueEnd - valueStart);
                    }
                }

                return meta;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Patch for PlaceableStorageEntity.Start - raises OnStorageCreated event.
        /// This fires when storage items are placed in the world.
        /// Note: In multiplayer, ItemInstance may be null on clients when Start() runs
        /// because the network RPC hasn't arrived yet. The InitializeGridItem patch
        /// handles this case.
        /// </summary>
        [HarmonyPatch(typeof(S1ObjectScripts.PlaceableStorageEntity), "Start")]
        [HarmonyPostfix]
        private static void PlaceableStorageEntity_Start_Postfix(S1ObjectScripts.PlaceableStorageEntity __instance)
        {
            // Skip if ItemInstance isn't ready yet - InitializeGridItem patch will handle it
            try
            {
                if (__instance?.ItemInstance?.Definition == null)
                    return;
            }
            catch (Exception)
            {
                // Accessing Definition accesses the ID, which can throw NRE
                return;
            }

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
        /// Patch for PlaceableStorageEntity.InitializeGridItem - handles multiplayer case.
        /// In multiplayer, when a client places storage, Start() runs before the ItemInstance
        /// is set via network RPC. This patch fires after InitializeGridItem is called,
        /// which is when ItemInstance is guaranteed to be set on both host and client.
        /// </summary>
        [HarmonyPatch(typeof(S1ObjectScripts.PlaceableStorageEntity), nameof(S1ObjectScripts.PlaceableStorageEntity.InitializeGridItem))]
        [HarmonyPostfix]
        private static void PlaceableStorageEntity_InitializeGridItem_Postfix(S1ObjectScripts.PlaceableStorageEntity __instance)
        {
            if (__instance?.ItemInstance?.Definition == null)
                return;

            if (__instance.StorageEntity == null)
                return;

            // Check if already processed by Start() patch
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
                Logger.Error($"Error in PlaceableStorageEntity_InitializeGridItem_Postfix: {ex.Message}");
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
        [HarmonyPatch(typeof(S1PersistenceLoaders.PlaceableStorageEntityLoader), "Load", new Type[] { typeof(S1Persistence.DynamicSaveData) })]
        [HarmonyPrefix]
        private static bool PlaceableStorageEntityLoader_Load_Prefix(S1PersistenceLoaders.PlaceableStorageEntityLoader __instance, S1Persistence.DynamicSaveData data)
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
                        // Parse manually to avoid IL2CPP generic method issues with JsonUtility.FromJson<T>
                        var meta = ParseStorageSlotMeta(metaJson);
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
                        string itemId = placeableStorage?.ItemInstance?.Definition?.ID ?? "unknown";

                        Logger.Warning(
                            $"Failed to deserialize storage slot metadata for item '{itemId}'. " +
                            $"This usually happens when a mod that added extra storage rows is no longer loaded. " +
                            $"The save file contains expanded storage data, but the mod that created it is missing. " +
                            $"Exception: {metaEx.GetType().Name}: {metaEx.Message}. "
                        );
                    }
                }

                // Expand slots before hydrating contents
                var wrapper = new StorageEntity(placeableStorage.StorageEntity, placeableStorage);
                wrapper.SetSlotCount(targetSlots);

                storageData.Contents.LoadTo(placeableStorage.StorageEntity.ItemSlots);

                // Load the Configuration (custom name) from save data.
                // The original loader does this deferred via onLoadComplete, but we apply it
                // directly since Configuration is already initialized after LoadAndCreate.
                if (data.TryGetData("Configuration", out string configJson) && !string.IsNullOrEmpty(configJson))
                {
                    try
                    {
                        var configName = ParseConfigurationName(configJson);
                        if (!string.IsNullOrEmpty(configName) && placeableStorage.Configuration?.Name != null)
                        {
                            placeableStorage.Configuration.Name.SetValue(configName, true);
                        }
                    }
                    catch (Exception configEx)
                    {
                        Logger.Warning($"Failed to load storage configuration name: {configEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in PlaceableStorageEntityLoader_Load_Prefix: {ex.Message}");
            }

            // Skip original loader to avoid double-loading
            return false;
        }

        /// <summary>
        /// Patch for StorageMenu.Open(StorageEntity) - raises OnStorageOpening event.
        /// This allows mods to sync custom names before the menu displays.
        /// </summary>
        [HarmonyPatch(typeof(S1UI.StorageMenu), nameof(S1UI.StorageMenu.Open), new Type[] { typeof(S1Storage.StorageEntity) })]
        [HarmonyPrefix]
        private static void StorageMenu_Open_Prefix(S1Storage.StorageEntity entity)
        {
            if (entity == null)
                return;

            try
            {
                // Get the placeable storage entity (if this is placeable storage)
                var placeableStorage = entity.GetComponentInParent<S1ObjectScripts.PlaceableStorageEntity>();

                // Wrap and raise event
                var storageWrapper = new StorageEntity(entity, placeableStorage);
                var args = new StorageEventArgs(storageWrapper);
                StorageEvents.RaiseStorageOpening(args);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in StorageMenu_Open_Prefix: {ex.Message}");
            }
        }
    }
}
