#if (IL2CPPMELON)
using S1Building = Il2CppScheduleOne.Building;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1ObjectScripts = Il2CppScheduleOne.ObjectScripts;
using S1EntityFramework = Il2CppScheduleOne.EntityFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Building = ScheduleOne.Building;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1ObjectScripts = ScheduleOne.ObjectScripts;
using S1EntityFramework = ScheduleOne.EntityFramework;
#endif

using HarmonyLib;
using S1API.Building;
using S1API.Items;
using S1API.Storage;
using S1API.Logging;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Harmony patches for the building system.
    /// Intercepts item placement to raise BuildEvents for modder consumption.
    /// </summary>
    [HarmonyPatch]
    internal static class BuildingPatches
    {
        private static readonly Log Logger = new Log("BuildingPatches");

        /// <summary>
        /// Patch for BuildManager.CreateGridItem - raises OnGridItemCreated event.
        /// </summary>
        [HarmonyPatch(typeof(S1Building.BuildManager), nameof(S1Building.BuildManager.CreateGridItem))]
        [HarmonyPostfix]
        private static void CreateGridItem_Postfix(S1EntityFramework.GridItem __result, S1ItemFramework.ItemInstance item)
        {
            if (__result == null || item?.Definition == null)
                return;

            try
            {
                var itemInstance = new ItemInstance(item);

                // Check for storage component
                var placeableStorage = __result.GetComponent<S1ObjectScripts.PlaceableStorageEntity>();
                StorageEntity storageWrapper = null;
                if (placeableStorage?.StorageEntity != null)
                {
                    storageWrapper = new StorageEntity(placeableStorage.StorageEntity, placeableStorage);
                }

                var args = new BuildEventArgs(itemInstance, __result.gameObject, storageWrapper);
                BuildEvents.RaiseGridItemCreated(args);
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Error in CreateGridItem_Postfix: {ex.Message}");
            }
        }

        /// <summary>
        /// Patch for BuildManager.CreateSurfaceItem - raises OnSurfaceItemCreated event.
        /// </summary>
        [HarmonyPatch(typeof(S1Building.BuildManager), nameof(S1Building.BuildManager.CreateSurfaceItem))]
        [HarmonyPostfix]
        private static void CreateSurfaceItem_Postfix(S1EntityFramework.SurfaceItem __result, S1ItemFramework.ItemInstance item)
        {
            if (__result == null || item?.Definition == null)
                return;

            try
            {
                var itemInstance = new ItemInstance(item);

                // Check for storage component
                var placeableStorage = __result.GetComponent<S1ObjectScripts.PlaceableStorageEntity>();
                StorageEntity storageWrapper = null;
                if (placeableStorage?.StorageEntity != null)
                {
                    storageWrapper = new StorageEntity(placeableStorage.StorageEntity, placeableStorage);
                }

                var args = new BuildEventArgs(itemInstance, __result.gameObject, storageWrapper);
                BuildEvents.RaiseSurfaceItemCreated(args);
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Error in CreateSurfaceItem_Postfix: {ex.Message}");
            }
        }

        /// <summary>
        /// Patch for BuildableItem.InitializeBuildableItem - raises OnBuildableItemInitialized event.
        /// </summary>
        [HarmonyPatch(typeof(S1EntityFramework.BuildableItem), nameof(S1EntityFramework.BuildableItem.InitializeBuildableItem))]
        [HarmonyPostfix]
        private static void InitializeBuildableItem_Postfix(S1EntityFramework.BuildableItem __instance, S1ItemFramework.ItemInstance instance)
        {
            if (__instance == null || instance?.Definition == null)
                return;

            try
            {
                var itemInstance = new ItemInstance(instance);

                // Check for storage component
                var placeableStorage = __instance.GetComponent<S1ObjectScripts.PlaceableStorageEntity>();
                StorageEntity storageWrapper = null;
                if (placeableStorage?.StorageEntity != null)
                {
                    storageWrapper = new StorageEntity(placeableStorage.StorageEntity, placeableStorage);
                }

                var args = new BuildEventArgs(itemInstance, __instance.gameObject, storageWrapper);
                BuildEvents.RaiseBuildableItemInitialized(args);
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Error in InitializeBuildableItem_Postfix: {ex.Message}");
            }
        }
    }
}
