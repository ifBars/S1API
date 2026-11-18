#if IL2CPPMELON
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
using S1Loaders = Il2CppScheduleOne.Persistence.Loaders;
using S1Heatmap = Il2CppScheduleOne.Heatmap;
using S1Property = Il2CppScheduleOne.Property;
#elif MONOMELON
using S1Datas = ScheduleOne.Persistence.Datas;
using S1Loaders = ScheduleOne.Persistence.Loaders;
using S1Heatmap = ScheduleOne.Heatmap;
using S1Property = ScheduleOne.Property;
#endif

using System;
using System.Collections.Generic;
using HarmonyLib;
using S1API.Internal.Properties;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Routes the vanilla save, load and heatmap code through <see cref="CustomProperties"/>. Every patch runs in
    /// try/catch and falls back to vanilla behaviour when it fails.
    /// </summary>
    [HarmonyPatch]
    internal static class CustomPropertyPatches
    {
        private static readonly Logging.Log Logger = new Logging.Log("CustomPropertyPatches");

        // Vanilla deletes every file in the Properties and Businesses folders that the manager did not list.
        [HarmonyPatch(typeof(S1Property.PropertyManager), nameof(S1Property.PropertyManager.WriteData))]
        [HarmonyPostfix]
        private static void PropertyWriteDataPostfix(S1Property.PropertyManager __instance, string parentFolderPath,
#if IL2CPPMELON
            Il2CppSystem.Collections.Generic.List<string> __result)
#else
            System.Collections.Generic.List<string> __result)
#endif
        {
            Keep("property", __instance != null ? __instance.SaveFolderName : null, parentFolderPath, __result);
        }

        [HarmonyPatch(typeof(S1Property.BusinessManager), nameof(S1Property.BusinessManager.WriteData))]
        [HarmonyPostfix]
        private static void BusinessWriteDataPostfix(S1Property.BusinessManager __instance, string parentFolderPath,
#if IL2CPPMELON
            Il2CppSystem.Collections.Generic.List<string> __result)
#else
            System.Collections.Generic.List<string> __result)
#endif
        {
            Keep("business", __instance != null ? __instance.SaveFolderName : null, parentFolderPath, __result);
        }

#if IL2CPPMELON
        private static void Keep(string kind, string? saveFolderName, string parentFolderPath, Il2CppSystem.Collections.Generic.List<string> result)
#else
        private static void Keep(string kind, string? saveFolderName, string parentFolderPath, System.Collections.Generic.List<string> result)
#endif
        {
            try
            {
                if (result == null || saveFolderName == null)
                {
                    Logger.Warning($"The {kind} manager listed no files; saved files of custom properties are not protected.");
                    return;
                }
                CustomProperties.KeepSavedFiles(kind, saveFolderName, parentFolderPath, name => result.Add(name));
            }
            catch (Exception ex)
            {
                Logger.Error($"Keeping saved {kind} files of custom properties failed; the save may delete them: {ex}");
            }
        }

        // Legacy folder saves: the property file sits in a folder of its own.
        [HarmonyPatch(typeof(S1Loaders.PropertyLoader), nameof(S1Loaders.PropertyLoader.Load), new[] { typeof(string) })]
        [HarmonyPrefix]
        private static bool LoadFolderPrefix(S1Loaders.PropertyLoader __instance, string mainPath)
        {
            try
            {
                var code = CustomProperties.ReadSavedCode(mainPath);
                return !CustomProperties.TryDeferLoad(code, $"folder '{mainPath}'", () => __instance.Load(mainPath));
            }
            catch (Exception ex)
            {
                Logger.Error($"Checking the property folder '{mainPath}' failed, vanilla loads it now: {ex}");
                return true;
            }
        }

        // Dynamic saves: the property data is already parsed.
        [HarmonyPatch(typeof(S1Loaders.PropertyLoader), nameof(S1Loaders.PropertyLoader.Load), new[] { typeof(S1Datas.PropertyData), typeof(string) })]
        [HarmonyPrefix]
        private static bool LoadDataPrefix(S1Loaders.PropertyLoader __instance, S1Datas.PropertyData propertyData, string propertDataString)
        {
            try
            {
                if (propertyData == null)
                    return true;
                return !CustomProperties.TryDeferLoad(propertyData.PropertyCode, "property data", () => __instance.Load(propertyData, propertDataString));
            }
            catch (Exception ex)
            {
                Logger.Error($"Checking the property data failed, vanilla loads it now: {ex}");
                return true;
            }
        }

        // The heatmap sets up the temperature view of every property that exists when it starts, and never again.
        [HarmonyPatch(typeof(S1Heatmap.HeatmapManager), "Start")]
        [HarmonyPrefix]
        private static bool HeatmapStartPrefix(S1Heatmap.HeatmapManager __instance)
        {
            try
            {
                if (CustomProperties.IsStartingHeatmap)
                    return true;
                return !CustomProperties.TryDeferHeatmap(() => StartHeatmap(__instance), () => __instance != null);
            }
            catch (Exception ex)
            {
                Logger.Error($"Checking the heatmap start failed, vanilla starts it now: {ex}");
                return true;
            }
        }

        // Vanilla indexes its heatmap data by property code without a check. A registered property the heatmap started
        // without (or that is still waiting for it) would throw, so its toggle is skipped.
        [HarmonyPatch(typeof(S1Heatmap.HeatmapManager), nameof(S1Heatmap.HeatmapManager.ToggleHeatmapActive))]
        [HarmonyPrefix]
        private static bool ToggleHeatmapPrefix(S1Property.Property property)
        {
            try
            {
                if (property == null)
                    return true;
                var code = property.PropertyCode;
                if (CustomProperties.HeatmapDeferred || CustomProperties.IsMissingFromHeatmap(code))
                {
                    if (CustomProperties.IsRegistered(code))
                        Logger.Warning($"Heatmap toggle for '{code}' skipped: the heatmap has no temperature view for it.");
                    return !CustomProperties.IsRegistered(code);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Checking the heatmap toggle failed, vanilla runs it: {ex}");
                return true;
            }
        }

        // A registered property whose load is deferred or was given up holds only part of its saved data. The two
        // WriteData methods write every property in the static lists, so such a property is taken out of the lists for
        // the call and put back at its old index afterwards; the WriteData postfix then keeps its saved file.
        // Property.ShouldSave is not patched: it is a bare "return true", and in IL2CPP a detour on it also caught
        // other methods with the same native body and crashed the boot.
        private static List<Action>? _pulledForSave;

        [HarmonyPatch(typeof(S1Property.PropertyManager), nameof(S1Property.PropertyManager.WriteData))]
        [HarmonyPrefix]
        private static void PropertyWriteDataPrefix()
        {
            PullUnfinished(() =>
            {
                Pull(S1Property.Property.OwnedProperties, "owned properties");
                Pull(S1Property.Property.UnownedProperties, "unowned properties");
            });
        }

        [HarmonyPatch(typeof(S1Property.PropertyManager), nameof(S1Property.PropertyManager.WriteData))]
        [HarmonyFinalizer]
        private static void PropertyWriteDataFinalizer() => PutBackPulled();

        [HarmonyPatch(typeof(S1Property.BusinessManager), nameof(S1Property.BusinessManager.WriteData))]
        [HarmonyPrefix]
        private static void BusinessWriteDataPrefix()
        {
            PullUnfinished(() =>
            {
                Pull(S1Property.Business.OwnedBusinesses, "owned businesses");
                Pull(S1Property.Business.UnownedBusinesses, "unowned businesses");
            });
        }

        [HarmonyPatch(typeof(S1Property.BusinessManager), nameof(S1Property.BusinessManager.WriteData))]
        [HarmonyFinalizer]
        private static void BusinessWriteDataFinalizer() => PutBackPulled();

        private static void PullUnfinished(Action pull)
        {
            try
            {
                if (_pulledForSave != null)
                {
                    Logger.Warning("Properties taken out for the previous save were not put back; putting them back now.");
                    PutBackPulled();
                }
                if (!CustomProperties.HasUnfinishedLoads())
                    return;
                pull();
            }
            catch (Exception ex)
            {
                Logger.Error($"Taking unfinished custom properties out of the save failed; they are saved as vanilla does: {ex}");
                PutBackPulled();
            }
        }

#if IL2CPPMELON
        private static void Pull<T>(Il2CppSystem.Collections.Generic.List<T> list, string what) where T : S1Property.Property
#else
        private static void Pull<T>(List<T> list, string what) where T : S1Property.Property
#endif
        {
            if (list == null)
            {
                Logger.Warning($"The game's list of {what} is missing; unfinished custom properties in it are not protected.");
                return;
            }
            for (int i = list.Count - 1; i >= 0; i--)
            {
                T property = list[i];
                if (property == null)
                    continue;
                string code = property.PropertyCode;
                if (!CustomProperties.IsLoadUnfinished(code))
                    continue;
                list.RemoveAt(i);
                int index = i;
                (_pulledForSave ??= new List<Action>()).Add(() => list.Insert(Math.Min(index, list.Count), property));
                Logger.Warning($"Custom property '{code}' is not saved now: its load has not finished, its saved file stays as it is.");
            }
        }

        private static void PutBackPulled()
        {
            var pulled = _pulledForSave;
            _pulledForSave = null;
            if (pulled == null)
                return;
            // Pulled from the end of each list, so the reverse order puts every entry back at its old index.
            for (int i = pulled.Count - 1; i >= 0; i--)
            {
                try { pulled[i](); }
                catch (Exception ex) { Logger.Error($"Putting an unfinished custom property back into its list failed: {ex}"); }
            }
            Logger.Msg($"{pulled.Count} unfinished custom propert(ies) put back after the save.");
        }

        // Start is protected in the Mono assembly, so it is invoked by name.
        private static void StartHeatmap(S1Heatmap.HeatmapManager heatmap)
        {
            var start = AccessTools.Method(typeof(S1Heatmap.HeatmapManager), "Start");
            if (start == null)
                throw new MissingMethodException(nameof(S1Heatmap.HeatmapManager), "Start");
            start.Invoke(heatmap, null);
        }
    }
}
