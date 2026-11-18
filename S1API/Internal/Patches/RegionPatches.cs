#if (IL2CPPMELON)
using S1Map = Il2CppScheduleOne.Map;
using S1Cartel = Il2CppScheduleOne.Cartel;
using S1Levelling = Il2CppScheduleOne.Levelling;
using S1ContactsApp = Il2CppScheduleOne.UI.Phone.ContactsApp;
using S1Persistence = Il2CppScheduleOne.Persistence;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#elif MONOMELON
using S1Map = ScheduleOne.Map;
using S1Cartel = ScheduleOne.Cartel;
using S1Levelling = ScheduleOne.Levelling;
using S1ContactsApp = ScheduleOne.UI.Phone.ContactsApp;
using S1Persistence = ScheduleOne.Persistence;
#endif

using System;
using HarmonyLib;
using S1API.Internal.Map;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Makes the custom map regions work in the vanilla map, cartel and contacts code, and keeps them out
    /// of the vanilla save strings. Every patch does nothing unless a custom region is registered.
    /// </summary>
    [HarmonyPatch]
    internal static class RegionPatches
    {
        [HarmonyPatch(typeof(S1Map.Map), "Awake")]
        [HarmonyPostfix]
        private static void MapAwake_Postfix(S1Map.Map __instance)
        {
            if (!CustomRegionRegistry.HasEntries) return;
            try
            {
                CustomRegionWorld.AddToMap(__instance);
            }
            catch (Exception ex)
            {
                CustomRegionRegistry.Error($"Adding the custom regions to the map failed: {ex}");
            }
        }

        [HarmonyPatch(typeof(S1Cartel.CartelInfluence), "Awake")]
        [HarmonyPrefix]
        private static void CartelInfluenceAwake_Prefix(S1Cartel.CartelInfluence __instance)
        {
            if (!CustomRegionRegistry.HasEntries) return;
            try
            {
                CustomRegionWorld.AddInfluenceData(__instance);
            }
            catch (Exception ex)
            {
                CustomRegionRegistry.Error($"Adding cartel influence data for the custom regions failed: {ex}");
            }
        }

        [HarmonyPatch(typeof(S1Cartel.CartelInfluence), "RpcLogic___ChangeInfluence_1267088319")]
        [HarmonyPrefix]
        private static bool ChangeInfluence_Prefix(S1Cartel.CartelInfluence __instance, S1Map.EMapRegion region, float oldInfluence, float newInfluence)
        {
            if (!CustomRegionRegistry.HasEntries) return true;
            return !CustomRegionWorld.ApplyInfluenceChange(__instance, region, oldInfluence, newInfluence);
        }

        [HarmonyPatch(typeof(S1Cartel.CartelActivities), "GetInfluenceFraction")]
        [HarmonyPrefix]
        private static bool InfluenceFraction_Prefix(ref float __result)
        {
            if (!CustomRegionWorld.TryInfluenceFraction(out float value)) return true;
            __result = value;
            return false;
        }

        [HarmonyPatch(typeof(S1Cartel.CartelActivities), "GetRegionalActivities")]
        [HarmonyPrefix]
        private static bool RegionalActivities_Prefix(S1Cartel.CartelActivities __instance, S1Map.EMapRegion region, ref S1Cartel.CartelRegionActivities __result)
        {
            if (!CustomRegionRegistry.HasEntries) return true;
            if (!CustomRegionWorld.TryRegionalActivities(__instance, region, out S1Cartel.CartelRegionActivities? activities)) return true;
            __result = activities!;
            return false;
        }

        [HarmonyPatch(typeof(S1Cartel.CartelInfluence), "GetAllRegionInfluence")]
        [HarmonyPostfix]
#if (IL2CPPMELON)
        private static void GetAllRegionInfluence_Postfix(ref Il2CppReferenceArray<S1Cartel.CartelInfluence.RegionInfluenceData> __result)
#else
        private static void GetAllRegionInfluence_Postfix(ref S1Cartel.CartelInfluence.RegionInfluenceData[] __result)
#endif
        {
            if (!CustomRegionRegistry.HasEntries) return;
            try
            {
                __result = CustomRegionWorld.StripInfluence(__result);
            }
            catch (Exception ex)
            {
                CustomRegionRegistry.Error($"Filtering the cartel save failed; it holds the custom regions: {ex}");
            }
        }

        [HarmonyPatch(typeof(S1Levelling.LevelManager), "GetSaveString")]
        [HarmonyPostfix]
        private static void LevelGetSaveString_Postfix(S1Levelling.LevelManager __instance, ref string __result)
        {
            string? stripped = CustomRegionWorld.StripRankSave(__instance);
            if (stripped != null) __result = stripped;
        }

        [HarmonyPatch(typeof(S1ContactsApp.ContactsApp), "Start")]
        [HarmonyPrefix]
        private static void ContactsAppStart_Prefix(S1ContactsApp.ContactsApp __instance)
        {
            if (!CustomRegionRegistry.HasEntries) return;
            try
            {
                CustomRegionWorld.AddContactsTabs(__instance);
            }
            catch (Exception ex)
            {
                CustomRegionRegistry.Error($"Adding the custom region tabs to the contacts app failed; their people are not on the relationship map: {ex}");
            }
        }

        [HarmonyPatch(typeof(S1ContactsApp.ContactsApp), "SetSelectedRegion")]
        [HarmonyPrefix]
        private static bool ContactsSetSelectedRegion_Prefix(S1ContactsApp.ContactsApp __instance, S1Map.EMapRegion region)
        {
            if (!CustomRegionRegistry.HasEntries) return true;
            return !CustomRegionWorld.TrySelectRegion(__instance, region);
        }

        [HarmonyPatch(typeof(S1Map.Map), "OnRankUp")]
        [HarmonyPostfix]
        private static void MapOnRankUp_Postfix(S1Levelling.FullRank newRank)
        {
            if (!CustomRegionRegistry.HasEntries) return;
            try
            {
                CustomRegionWorld.EvaluateRankUnlocks((int)newRank.Rank, announce: true);
            }
            catch (Exception ex)
            {
                CustomRegionRegistry.Error($"Unlocking custom regions by rank failed: {ex}");
            }
        }

        /// <summary>The rank save loader sets the rank through this call; regions for a rank already held unlock then.</summary>
        [HarmonyPatch(typeof(S1Levelling.LevelManager), "SetData")]
        [HarmonyPostfix]
        private static void LevelSetData_Postfix(S1Levelling.ERank rank)
        {
            if (!CustomRegionRegistry.HasEntries) return;
            try
            {
                CustomRegionWorld.EvaluateRankUnlocks((int)rank, announce: false);
            }
            catch (Exception ex)
            {
                CustomRegionRegistry.Error($"Unlocking custom regions for the loaded rank failed: {ex}");
            }
        }

        [HarmonyPatch(typeof(S1Persistence.SaveManager), "Save", new Type[] { typeof(string) })]
        [HarmonyPostfix]
        private static void SaveManagerSave_Postfix(string saveFolderPath)
        {
            if (!CustomRegionRegistry.HasEntries) return;
            try
            {
                var saveManager = S1Persistence.SaveManager.Instance;
                if (!saveManager.ApprovedBaseLevelPaths.Contains("Modded"))
                    saveManager.ApprovedBaseLevelPaths.Add("Modded");
                CustomRegionWorld.Save(saveFolderPath);
            }
            catch (Exception ex)
            {
                CustomRegionRegistry.Error($"Saving the custom regions failed; their state is lost for this save: {ex}");
            }
        }
    }
}
