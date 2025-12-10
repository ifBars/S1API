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
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in ItemSet_LoadTo_Prefix: {ex.Message}");
            }
        }
    }
}
