#if IL2CPPMELON
using S1Persistence = Il2CppScheduleOne.Persistence;
#elif MONOMELON
using S1Persistence = ScheduleOne.Persistence;
#endif

using System;
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

        // The game saves through both overloads, and the short one can be inlined into its callers.
        [HarmonyPatch(typeof(S1Persistence.SaveManager), nameof(S1Persistence.SaveManager.Save), new Type[] { })]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool SavePrefix()
        {
            try { return SaveLoadGates.AllowSave("Save"); }
            catch (Exception ex) { Logger.Error($"Save veto check failed, the save runs: {ex}"); return true; }
        }

        [HarmonyPatch(typeof(S1Persistence.SaveManager), nameof(S1Persistence.SaveManager.Save), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool SaveToFolderPrefix()
        {
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
