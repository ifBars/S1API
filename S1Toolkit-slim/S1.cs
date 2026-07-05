using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using MelonLoader;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppFishNet;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.ItemFramework;
using MelonLoader.Utils;
using UnityEngine;

// Required assembly-level attributes for MelonLoader
[assembly: MelonGame("TVGS", "Schedule I")]

namespace S1Toolkit
{
    /// <summary>
    /// Static helper functions for Schedule I MelonLoader mods.
    /// Il2Cpp-compatible, null-safe, stateless, no abstraction.
    /// Each method does exactly one thing – understandable even for weak LLMs.
    /// </summary>
    public static class S1
    {
        // ──────────────────────────────────────────────
        // 1. Il2Cpp casting & null safety
        // ──────────────────────────────────────────────

        /// <summary>Il2Cpp-safe cast. Uses TryCast instead of "as" or "(T)".</summary>
        public static T TryCast<T>(Il2CppObjectBase obj) where T : Il2CppObjectBase
        {
            if (obj == null) return null;
            return obj.TryCast<T>();
        }

        /// <summary>Checks whether a UnityEngine.Object is still alive (Unity fake-null safe).</summary>
        public static bool IsAlive(UnityEngine.Object obj)
        {
            // Unity operator != null – checks the native pointer, not the managed proxy
            return obj != null;
        }

        /// <summary>Checks whether an Il2CppObjectBase is valid (not collected by the Il2Cpp GC).</summary>
        public static bool IsValid(Il2CppObjectBase obj)
        {
            return obj != null && !obj.WasCollected;
        }

        // ──────────────────────────────────────────────
        // 2. Game objects & player
        // ──────────────────────────────────────────────

        /// <summary>Returns the local player. Null when not in a session.</summary>
        public static Player LocalPlayer()
        {
            return Player.Local;
        }

        /// <summary>Searches the scene for an object with component T. Null if not found.</summary>
        public static T FindInScene<T>() where T : Component
        {
            return UnityEngine.Object.FindObjectOfType<T>();
        }

        /// <summary>Returns an item from the registry by ID. Null if not found.</summary>
        public static ItemDefinition GetItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            return Il2CppScheduleOne.Registry.GetItem(itemId);
        }

        // ──────────────────────────────────────────────
        // 3. Session/co-op detection
        // ──────────────────────────────────────────────

        /// <summary>Am I the server (singleplayer OR co-op host)?</summary>
        public static bool IsServer()
        {
            try { return InstanceFinder.IsServer; }
            catch { return true; } // fallback: singleplayer
        }

        /// <summary>Am I the host (server + local client)?</summary>
        public static bool IsHost()
        {
            try { return InstanceFinder.IsHost; }
            catch { return true; }
        }

        /// <summary>Am I a client only (co-op guest)?</summary>
        public static bool IsClientOnly()
        {
            try { return InstanceFinder.IsClientOnly; }
            catch { return false; }
        }

        /// <summary>More than one player in the world (co-op)?</summary>
        public static bool IsMultiplayer()
        {
            try { return Player.PlayerList.Count > 1; }
            catch { return false; }
        }

        // ──────────────────────────────────────────────
        // 4. Coroutine utilities
        // ──────────────────────────────────────────────

        /// <summary>Runs an action after a delay (seconds).</summary>
        public static void RunAfter(float seconds, Action action)
        {
            if (action == null) return;
            MelonCoroutines.Start(RunAfterCoroutine(seconds, action));
        }

        private static IEnumerator RunAfterCoroutine(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            try { action(); }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"[S1.RunAfter] Error: {ex}");
            }
        }

        /// <summary>Runs an action every frame until the condition is true.</summary>
        public static void RunUntil(Func<bool> condition, Action action)
        {
            if (condition == null || action == null) return;
            MelonCoroutines.Start(RunUntilCoroutine(condition, action));
        }

        private static IEnumerator RunUntilCoroutine(Func<bool> condition, Action action)
        {
            while (!condition())
            {
                try { action(); }
                catch (Exception ex)
                {
                    MelonLoader.MelonLogger.Error($"[S1.RunUntil] Error: {ex}");
                    yield break;
                }
                yield return null;
            }
        }

        /// <summary>Runs an action every N seconds, forever. Returns the coroutine token (pass to MelonCoroutines.Stop to cancel).</summary>
        public static object Every(float seconds, Action action)
        {
            if (action == null) return null;
            return MelonCoroutines.Start(EveryCoroutine(seconds, action));
        }

        private static IEnumerator EveryCoroutine(float seconds, Action action)
        {
            while (true)
            {
                yield return new WaitForSeconds(seconds);
                try { action(); }
                catch (Exception ex)
                {
                    MelonLoader.MelonLogger.Error($"[S1.Every] Error: {ex}");
                    yield break;
                }
            }
        }

        /// <summary>Runs an action each time a key is pressed down. Returns the coroutine token (pass to MelonCoroutines.Stop to cancel).</summary>
        public static object OnKey(KeyCode key, Action action)
        {
            if (action == null) return null;
            return MelonCoroutines.Start(OnKeyCoroutine(key, action));
        }

        private static IEnumerator OnKeyCoroutine(KeyCode key, Action action)
        {
            while (true)
            {
                if (Input.GetKeyDown(key))
                {
                    try { action(); }
                    catch (Exception ex)
                    {
                        MelonLoader.MelonLogger.Error($"[S1.OnKey] Error: {ex}");
                        yield break;
                    }
                }
                yield return null;
            }
        }

        // ──────────────────────────────────────────────
        // 5. Safe Execution (Circuit Breaker Light)
        // ──────────────────────────────────────────────

        private static readonly Dictionary<string, int> _errorCounts = new();
        private const int MaxErrors = 3;

        /// <summary>
        /// Runs code safely. After 3 errors the block is disabled.
        /// Ideal for Harmony patch bodies.
        /// </summary>
        public static void SafeRun(string name, Action body)
        {
            if (_errorCounts.TryGetValue(name, out int count) && count >= MaxErrors)
                return; // disabled after too many errors

            try
            {
                body();
            }
            catch (Exception ex)
            {
                count = _errorCounts.TryGetValue(name, out var c) ? c + 1 : 1;
                _errorCounts[name] = count;
                MelonLoader.MelonLogger.Error($"[S1.SafeRun:{name}] ({count}/{MaxErrors}) {ex}\n{ex.StackTrace}");

                if (count >= MaxErrors)
                    MelonLoader.MelonLogger.Error($"[S1.SafeRun:{name}] Disabled – too many errors.");
            }
        }

        /// <summary>Resets the error counter for a SafeRun block.</summary>
        public static void ResetSafeRun(string name)
        {
            _errorCounts.Remove(name);
        }

        // ──────────────────────────────────────────────
        // 6. File paths & save system
        // ──────────────────────────────────────────────

        /// <summary>Returns the base path for mod data: UserData/{ModName}/</summary>
        public static string GetModDataPath(string modName)
        {
            return Path.Combine(MelonEnvironment.UserDataDirectory, modName);
        }

        /// <summary>Returns the path to the saves folder: UserData/{ModName}/Saves/</summary>
        public static string GetSavesPath(string modName)
        {
            return Path.Combine(GetModDataPath(modName), "Saves");
        }

        /// <summary>Returns the current save ID. Null when no save is loaded yet.</summary>
        public static string GetCurrentSaveId()
        {
            try
            {
                if (SaveManager.Instance != null)
                    return SaveManager.Instance.SaveName;
            }
            catch { }
            return null;
        }

        /// <summary>Returns the full path to the save file.</summary>
        public static string GetSaveFilePath(string modName)
        {
            var saveId = GetCurrentSaveId();
            if (string.IsNullOrEmpty(saveId)) return null;
            var savesPath = GetSavesPath(modName);
            Directory.CreateDirectory(savesPath);
            return Path.Combine(savesPath, $"{saveId}.json");
        }

        // ──────────────────────────────────────────────
        // 7. Config utilities (MelonPreferences wrapper)
        // ──────────────────────────────────────────────

        /// <summary>Creates or reads a config entry with a default value.</summary>
        public static MelonPreferences_Entry<T> GetConfig<T>(
            string categoryName, string categoryDisplayName,
            string entryName, T defaultValue, string description = "")
        {
            var category = MelonPreferences.CreateCategory(categoryName, categoryDisplayName);
            category.LoadFromFile();
            return category.CreateEntry(entryName, defaultValue, entryName, description);
        }

        /// <summary>Saves all changes in a config category.</summary>
        public static void SaveConfig(string categoryName)
        {
            var category = MelonPreferences.GetCategory(categoryName);
            category?.SaveToFile(false);
        }
    }
}
