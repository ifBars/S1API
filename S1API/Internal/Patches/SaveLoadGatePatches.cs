#if IL2CPPMELON
using S1Persistence = Il2CppScheduleOne.Persistence;
#elif MONOMELON
using S1Persistence = ScheduleOne.Persistence;
#endif

using System;
using System.IO;
using HarmonyLib;
using S1API.Internal.Lifecycle;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Connects both save overloads and the two load entry points to <see cref="SaveLoadGates"/>. The prefixes run
    /// first, so other prefixes and the S1API save postfixes can see a blocked save or a deferred load through
    /// <c>__runOriginal</c>.
    /// </summary>
    [HarmonyPatch]
    internal static class SaveLoadGatePatches
    {
        private static readonly Logging.Log Logger = new Logging.Log("SaveLoadGatePatches");

        // SaveManager.Save() calls Save(string) itself. Save() asks the vetoes once and holds the answer while it
        // runs, so the nested Save(string) call does not ask again. Callers that pass a path to Save(string) directly
        // are asked there.
        private static bool _insideShortSave;

        [HarmonyPatch(typeof(S1Persistence.SaveManager), nameof(S1Persistence.SaveManager.Save), new Type[] { })]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool SavePrefix()
        {
            _insideShortSave = false;
            try
            {
                bool allowed = SaveLoadGates.AllowSave("Save");
                _insideShortSave = allowed;
                return allowed;
            }
            catch (Exception ex)
            {
                Logger.Error($"Save veto check failed, the save runs: {ex}");
                _insideShortSave = true;
                return true;
            }
        }

        [HarmonyPatch(typeof(S1Persistence.SaveManager), nameof(S1Persistence.SaveManager.Save), new Type[] { })]
        [HarmonyFinalizer]
        private static void SaveFinalizer() => _insideShortSave = false;

        [HarmonyPatch(typeof(S1Persistence.SaveManager), nameof(S1Persistence.SaveManager.Save), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool SaveToFolderPrefix()
        {
            // Save() already asked the vetoes for this save and allowed it.
            if (_insideShortSave)
                return true;

            try { return SaveLoadGates.AllowSave("Save"); }
            catch (Exception ex) { Logger.Error($"Save veto check failed, the save runs: {ex}"); return true; }
        }

        [HarmonyPatch(typeof(S1Persistence.LoadManager), nameof(S1Persistence.LoadManager.StartGame))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool StartGamePrefix(S1Persistence.LoadManager __instance, S1Persistence.SaveInfo info, bool allowLoadStacking, bool allowSaveBackup)
        {
            try
            {
                // Vanilla rejects these calls at once; deferring them would delay the rejection by the gate wait.
                if (info == null || (__instance.IsGameLoaded && !allowLoadStacking) || !Directory.Exists(info.SavePath))
                {
                    Logger.Msg("StartGame is not deferred: vanilla rejects it at once.");
                    return true;
                }

                return !SaveLoadGates.TryDefer("StartGame", () => __instance.StartGame(info, allowLoadStacking, allowSaveBackup));
            }
            catch (Exception ex)
            {
                Logger.Error($"Load gate check for StartGame failed, the load runs: {ex}");
                return true;
            }
        }

        [HarmonyPatch(typeof(S1Persistence.LoadManager), nameof(S1Persistence.LoadManager.LoadAsClient))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool LoadAsClientPrefix(S1Persistence.LoadManager __instance, string steamId64)
        {
            try
            {
                return !SaveLoadGates.TryDefer("LoadAsClient", () => __instance.LoadAsClient(steamId64));
            }
            catch (Exception ex)
            {
                Logger.Error($"Load gate check for LoadAsClient failed, the load runs: {ex}");
                return true;
            }
        }
    }
}
