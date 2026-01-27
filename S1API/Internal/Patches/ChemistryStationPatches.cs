#if (IL2CPPMELON)
using S1ObjectScripts = Il2CppScheduleOne.ObjectScripts;
using S1StationFramework = Il2CppScheduleOne.StationFramework;
using S1UIStations = Il2CppScheduleOne.UI.Stations;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ObjectScripts = ScheduleOne.ObjectScripts;
using S1StationFramework = ScheduleOne.StationFramework;
using S1UIStations = ScheduleOne.UI.Stations;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using S1API.Internal.Utils;
using S1API.Logging;
using S1API.Stations;
using UnityEngine;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Harmony patches to inject S1API-registered Chemistry Station recipes into the UI.
    /// </summary>
    [HarmonyPatch]
    internal static class ChemistryStationPatches
    {
        private static readonly Log Logger = new Log("ChemistryStationPatches");
        private static readonly HashSet<string> LoggedCanvasConflicts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> LoggedEntryConflicts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static FieldInfo? _recipeEntriesField;
        private static bool _recipeEntriesFieldLookupAttempted;
        private static bool _loggedRecipeEntriesFieldDump;

        private static FieldInfo? GetRecipeEntriesField()
        {
            if (_recipeEntriesFieldLookupAttempted)
                return _recipeEntriesField;

            _recipeEntriesFieldLookupAttempted = true;

            // Mono: field is named "recipeEntries"
            // IL2CPP: field name can differ; find by type instead of name.
            try
            {
                var byName = AccessTools.Field(typeof(S1UIStations.ChemistryStationCanvas), "recipeEntries");
                if (byName != null)
                {
                    _recipeEntriesField = byName;
                    return _recipeEntriesField;
                }
            }
            catch
            {
                // ignore and continue with scan
            }

            try
            {
                foreach (var field in AccessTools.GetDeclaredFields(typeof(S1UIStations.ChemistryStationCanvas)))
                {
                    if (field == null)
                        continue;

                    var ft = field.FieldType;
                    if (ft == null || !ft.IsGenericType)
                        continue;

                    var genDef = ft.GetGenericTypeDefinition();
                    if (genDef == null)
                        continue;

                    // Support both System.Collections.Generic.List<T> and Il2CppSystem.Collections.Generic.List<T>
                    if (!string.Equals(genDef.FullName, "System.Collections.Generic.List`1", StringComparison.Ordinal)
                        && !string.Equals(genDef.FullName, "Il2CppSystem.Collections.Generic.List`1", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var args = ft.GetGenericArguments();
                    if (args == null || args.Length != 1)
                        continue;

                    if (args[0] == typeof(S1UIStations.StationRecipeEntry))
                    {
                        _recipeEntriesField = field;
                        return _recipeEntriesField;
                    }
                }
            }
            catch
            {
                // ignore
            }

            return _recipeEntriesField;
        }

        private static bool TryGetRecipeEntriesList(
            S1UIStations.ChemistryStationCanvas canvas,
#if (IL2CPPMELON || IL2CPPBEPINEX)
            out Il2CppSystem.Collections.Generic.List<S1UIStations.StationRecipeEntry>? entries
#else
            out List<S1UIStations.StationRecipeEntry>? entries
#endif
        )
        {
            entries = null;
            if (canvas == null)
                return false;

            // Mono: direct private field access is reliable.
#if (MONOMELON || MONOBEPINEX)
            var field = GetRecipeEntriesField();
            if (field != null)
            {
                try { entries = field.GetValue(canvas) as List<S1UIStations.StationRecipeEntry>; }
                catch { entries = null; }
            }

            return entries != null;
#else
            // IL2CPP: many private fields are exposed as generated properties on the proxy type.
            try
            {
                var byName = ReflectionUtils.TryGetFieldOrProperty(canvas, "recipeEntries");
                entries = byName as Il2CppSystem.Collections.Generic.List<S1UIStations.StationRecipeEntry>;
                if (entries != null)
                    return true;
            }
            catch { }

            // Fallback: scan instance properties for a List<StationRecipeEntry>
            try
            {
                var t = canvas.GetType();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                foreach (var prop in t.GetProperties(flags))
                {
                    if (prop == null || !prop.CanRead)
                        continue;

                    var pt = prop.PropertyType;
                    if (pt == null || !pt.IsGenericType)
                        continue;

                    var gen = pt.GetGenericTypeDefinition();
                    if (gen == null)
                        continue;

                    if (!string.Equals(gen.FullName, "Il2CppSystem.Collections.Generic.List`1", StringComparison.Ordinal)
                        && !string.Equals(gen.FullName, "System.Collections.Generic.List`1", StringComparison.Ordinal))
                        continue;

                    var args = pt.GetGenericArguments();
                    if (args.Length != 1 || args[0] != typeof(S1UIStations.StationRecipeEntry))
                        continue;

                    var val = prop.GetValue(canvas);
                    entries = val as Il2CppSystem.Collections.Generic.List<S1UIStations.StationRecipeEntry>;
                    if (entries != null)
                        return true;
                }
            }
            catch { }

            // Last chance: scan instance fields (rare on IL2CPP proxies, but cheap).
            try
            {
                var t = canvas.GetType();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                foreach (var f in t.GetFields(flags))
                {
                    if (f == null)
                        continue;

                    var ft = f.FieldType;
                    if (ft == null || !ft.IsGenericType)
                        continue;

                    var gen = ft.GetGenericTypeDefinition();
                    if (gen == null)
                        continue;

                    if (!string.Equals(gen.FullName, "Il2CppSystem.Collections.Generic.List`1", StringComparison.Ordinal)
                        && !string.Equals(gen.FullName, "System.Collections.Generic.List`1", StringComparison.Ordinal))
                        continue;

                    var args = ft.GetGenericArguments();
                    if (args.Length != 1 || args[0] != typeof(S1UIStations.StationRecipeEntry))
                        continue;

                    var val = f.GetValue(canvas);
                    entries = val as Il2CppSystem.Collections.Generic.List<S1UIStations.StationRecipeEntry>;
                    if (entries != null)
                        return true;
                }
            }
            catch { }

            return false;
#endif
        }

        [HarmonyPatch(typeof(S1UIStations.ChemistryStationCanvas), "Awake")]
        [HarmonyPrefix]
        private static void AwakePrefix(S1UIStations.ChemistryStationCanvas __instance)
        {
            try
            {
                InjectRegisteredRecipes(__instance);
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] ChemistryStationCanvas.Awake inject failed: {ex.Message}");
            }
        }

        [HarmonyPatch(typeof(S1UIStations.ChemistryStationCanvas), "Open")]
        [HarmonyPrefix]
        private static void OpenPrefix(S1UIStations.ChemistryStationCanvas __instance, S1ObjectScripts.ChemistryStation __0)
        {
            try
            {
                // Late registrations: ensure Recipes and recipeEntries are in sync before StationSlotsChanged runs.
                InjectRegisteredRecipes(__instance);
                EnsureRecipeEntries(__instance);
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] ChemistryStationCanvas.Open sync failed: {ex.Message}");
            }
        }

        private static void InjectRegisteredRecipes(S1UIStations.ChemistryStationCanvas canvas)
        {
            if (canvas == null)
                return;

            var registered = ChemistryStationRecipes.GetAllNative();
            if (registered == null || registered.Count == 0)
                return;

            var recipes = canvas.Recipes;
            if (recipes == null)
                return;

            for (int i = 0; i < registered.Count; i++)
            {
                var custom = registered[i];
                if (custom == null)
                    continue;

                string? id;
                try { id = custom.RecipeID; } catch { id = null; }
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (ContainsRecipeId(recipes, id))
                {
                    if (LoggedCanvasConflicts.Add(id))
                        Logger.Warning($"[S1API] Chemistry Station already has a recipe with ID '{id}'. Skipping S1API injection for this ID.");
                    continue;
                }

                recipes.Add(custom);
            }
        }

        private static void EnsureRecipeEntries(S1UIStations.ChemistryStationCanvas canvas)
        {
            if (canvas == null)
                return;

            if (!TryGetRecipeEntriesList(canvas, out var entries) || entries == null)
            {
                if (!_loggedRecipeEntriesFieldDump)
                {
                    _loggedRecipeEntriesFieldDump = true;
                    Logger.Warning("[S1API] ChemistryStationCanvas recipeEntries field could not be resolved. Late recipe UI sync will be skipped.");

                    // Best-effort field dump to aid IL2CPP troubleshooting.
                    try
                    {
                        var fields = AccessTools.GetDeclaredFields(typeof(S1UIStations.ChemistryStationCanvas));
                        foreach (var f in fields)
                        {
                            if (f == null)
                                continue;
                            Logger.Warning($"[S1API] ChemistryStationCanvas field: {f.Name} : {f.FieldType?.FullName}");
                        }
                    }
                    catch
                    {
                        // ignore
                    }

                    // Best-effort property dump (IL2CPP often exposes fields via properties).
                    try
                    {
                        var props = AccessTools.GetDeclaredProperties(typeof(S1UIStations.ChemistryStationCanvas));
                        foreach (var p in props)
                        {
                            if (p == null)
                                continue;
                            Logger.Warning($"[S1API] ChemistryStationCanvas property: {p.Name} : {p.PropertyType?.FullName}");
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }

                return;
            }

            var registered = ChemistryStationRecipes.GetAllNative();
            if (registered == null || registered.Count == 0)
                return;

            for (int i = 0; i < registered.Count; i++)
            {
                var recipe = registered[i];
                if (recipe == null)
                    continue;

                string? id;
                try { id = recipe.RecipeID; } catch { id = null; }
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (ContainsEntryForRecipeId(entries, id))
                {
                    if (LoggedEntryConflicts.Add(id))
                        Logger.Warning($"[S1API] Chemistry Station UI already has an entry for recipe ID '{id}'. Skipping entry injection for this ID.");
                    continue;
                }

                try
                {
                    if (canvas.RecipeEntryPrefab == null || canvas.RecipeContainer == null)
                        return;

                    var entry = UnityEngine.Object.Instantiate(canvas.RecipeEntryPrefab, canvas.RecipeContainer);
                    if (entry == null)
                        continue;

                    entry.AssignRecipe(recipe);
                    entries.Add(entry);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to create StationRecipeEntry for '{id}': {ex.Message}");
                }
            }
        }

        private static bool ContainsRecipeId(
#if (IL2CPPMELON || IL2CPPBEPINEX)
            Il2CppSystem.Collections.Generic.List<S1StationFramework.StationRecipe> recipes,
#else
            List<S1StationFramework.StationRecipe> recipes,
#endif
            string recipeId)
        {
            if (recipes == null || string.IsNullOrWhiteSpace(recipeId))
                return false;

            for (int i = 0; i < recipes.Count; i++)
            {
                var r = recipes[i];
                if (r == null)
                    continue;

                try
                {
                    if (string.Equals(r.RecipeID, recipeId, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                catch
                {
                    // ignore and continue
                }
            }

            return false;
        }

        private static bool ContainsEntryForRecipeId(
#if (IL2CPPMELON || IL2CPPBEPINEX)
            Il2CppSystem.Collections.Generic.List<S1UIStations.StationRecipeEntry> entries,
#else
            List<S1UIStations.StationRecipeEntry> entries,
#endif
            string recipeId)
        {
            if (entries == null || string.IsNullOrWhiteSpace(recipeId))
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null)
                    continue;

                try
                {
                    var r = e.Recipe;
                    if (r == null)
                        continue;

                    if (string.Equals(r.RecipeID, recipeId, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                catch
                {
                    // ignore and continue
                }
            }

            return false;
        }
    }
}
