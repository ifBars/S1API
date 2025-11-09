#if (IL2CPPMELON)
using S1Loaders = Il2CppScheduleOne.Persistence.Loaders;
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
using S1Quests = Il2CppScheduleOne.Quests;
using S1Persistence = Il2CppScheduleOne.Persistence;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Loaders = ScheduleOne.Persistence.Loaders;
using S1Datas = ScheduleOne.Persistence.Datas;
using S1Quests = ScheduleOne.Quests;
using S1Persistence = ScheduleOne.Persistence;
#endif
#if (IL2CPPMELON || IL2CPPBEPINEX)
using Il2CppSystem.Collections.Generic;
#elif (MONOMELON || MONOBEPINEX)
using System.Collections.Generic;
#endif

using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using S1API.Internal.Utils;
using S1API.Quests;
using UnityEngine;
using ISaveable = S1API.Internal.Abstraction.ISaveable;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Contains patches related to quest processing and custom modifications.
    /// </summary>
    [HarmonyPatch]
    internal class QuestPatches
    {
        /// <summary>
        /// Provides a centralized logging mechanism to capture and output messages, warnings,
        /// and errors during runtime, using underlying logging frameworks like BepInEx or MelonLoader.
        /// </summary>
        protected static readonly Logging.Log Logger = new Logging.Log("QuestPatches");

        /// <summary>
        /// Executes additional logic after quests are saved by the SaveManager.
        /// Ensures that directories for modded quests are properly created and that
        /// only non-vanilla modded quests are saved into the specified folder.
        /// </summary>
        /// <param name="saveFolderPath">The path to the save folder where quests are being stored.</param>
        [HarmonyPatch(typeof(S1Persistence.SaveManager), nameof(S1Persistence.SaveManager.Save), typeof(string))]
        [HarmonyPostfix]
        private static void SaveManager_Save_Postfix(string saveFolderPath)
        {
            try
            {
                var saveManager = S1Persistence.SaveManager.Instance;

                string[] approved = {
                    "Modded",
                    Path.Combine("Modded", "Quests")
                };

                foreach (var path in approved)
                {
                    if (!saveManager.ApprovedBaseLevelPaths.Contains(path))
                        saveManager.ApprovedBaseLevelPaths.Add(path);
                }

                string questsPath = Path.Combine(saveFolderPath, "Modded", "Quests");
                Directory.CreateDirectory(questsPath);

                try
                {
                    string[] existing = Directory.GetDirectories(questsPath);
                    for (int i = 0; i < existing.Length; i++)
                    {
                        string dirName = Path.GetFileName(existing[i]);
                        if (string.IsNullOrEmpty(dirName))
                            continue;

                        string guidStr = dirName.StartsWith("Quest_") && dirName.Length > 6 ? dirName.Substring(6) : string.Empty;
                        bool stillActive = false;
                        if (!string.IsNullOrEmpty(guidStr))
                        {
                            for (int q = 0; q < QuestManager.Quests.Count; q++)
                            {
                                var quest = QuestManager.Quests[q];
                                if (quest != null && quest.S1Quest != null && quest.S1Quest.StaticGUID == guidStr)
                                {
                                    stillActive = true;
                                    break;
                                }
                            }
                        }

                        if (!stillActive)
                        {
                            try { Directory.Delete(existing[i], true); } catch { /* ignore */ }
                        }
                    }
                }
                catch (Exception) { /* ignore cleanup errors */ }

                foreach (Quest quest in QuestManager.Quests)
                {
                    if (!quest.GetType().Namespace.StartsWith("ScheduleOne"))
                    {
                        List<string> dummy = new List<string>();
                        quest.SaveInternal(questsPath, ref dummy);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error("[S1API] ❌ Failed to save modded quests:\n" + ex);
            }
        }


        /// <summary>
        /// Invoked after all base quests are loaded to handle modded quest loading.
        /// Loads modded quests from a specific "Modded/Quests" directory and integrates them into the game.
        /// </summary>
        /// <param name="__instance">The quest loader instance responsible for managing quest load operations.</param>
        /// <param name="mainPath">The path to the primary quest directory in the base game.</param>
        [HarmonyPatch(typeof(S1Loaders.QuestsLoader), "Load")]
        [HarmonyPostfix]
        private static void QuestsLoaderLoad(S1Loaders.QuestsLoader __instance, string mainPath)
        {
            string moddedQuestsPath = Path.Combine(
                S1Persistence.LoadManager.Instance.LoadedGameFolderPath,
                "Modded", "Quests"
            );

            if (!Directory.Exists(moddedQuestsPath))
            {
                Directory.CreateDirectory(moddedQuestsPath);
                return;
            }

            string[] questDirectories = Directory.GetDirectories(moddedQuestsPath)
                .Select(Path.GetFileName)
                .Where(directory => directory != null && directory.StartsWith("Quest_"))
                .ToArray();

            foreach (string questDirectory in questDirectories)
            {
                string baseQuestPath = Path.Combine(moddedQuestsPath, questDirectory);
                __instance.TryLoadFile(baseQuestPath, out string questDataText);
                if (questDataText == null)
                    continue;

                S1Datas.QuestData baseQuestData = JsonUtility.FromJson<S1Datas.QuestData>(questDataText);

                string questDirectoryPath = Path.Combine(moddedQuestsPath, questDirectory);
                string questDataPath = Path.Combine(questDirectoryPath, "QuestData");
                if (!__instance.TryLoadFile(questDataPath, out string questText))
                    continue;

                QuestData? questData = JsonConvert.DeserializeObject<QuestData>(questText, ISaveable.SerializerSettings);
                if (questData?.ClassName == null)
                    continue;

                Type? questType = ReflectionUtils.GetTypeByName(questData.ClassName);
                if (questType == null || !typeof(Quest).IsAssignableFrom(questType))
                    continue;

                if (baseQuestData != null && baseQuestData.State != S1Quests.EQuestState.Active)
                {
                    continue;
                }

                Quest quest = QuestManager.CreateQuest(questType, baseQuestData?.GUID);
                quest.LoadInternal(questDirectoryPath);
            }
        }


        /// <summary>
        /// Executes logic prior to the start of a quest.
        /// Ensures that linked modded quest data is properly initialized.
        /// </summary>
        /// <param name="__instance">The instance of the quest that is being started.</param>
        [HarmonyPatch(typeof(S1Quests.Quest), "Start")]
        [HarmonyPrefix]
        private static void QuestStart(S1Quests.Quest __instance) =>
            QuestManager.Quests.FirstOrDefault(quest => quest.S1Quest == __instance)?.CreateInternal();

        /// <summary>
        /// Prevents compass element creation for quest entries that don't have a location.
        /// This ensures quest entries without POI positions don't create unwanted compass waypoints.
        /// </summary>
        /// <param name="__instance">The QuestEntry instance attempting to create a compass element.</param>
        /// <returns>False to skip the original method if PoILocation is null, true otherwise.</returns>
        [HarmonyPatch(typeof(S1Quests.QuestEntry), nameof(S1Quests.QuestEntry.CreateCompassElement))]
        [HarmonyPrefix]
        private static bool QuestEntry_CreateCompassElement_Prefix(S1Quests.QuestEntry __instance)
        {
            // Get PoILocation using reflection to handle both Mono (field) and IL2CPP (property)
            object? poILocation = ReflectionUtils.TryGetFieldOrProperty(__instance, "PoILocation");
            
            // Skip compass element creation if PoILocation is null
            // This prevents creating compass waypoints for quest entries without locations
            if (poILocation == null)
            {
                return false; // Skip the original method
            }
            
            return true; // Continue with the original method
        }

        /////// TODO: Quests doesn't have OnDestroy. Find another way to clean up
        // [HarmonyPatch(typeof(S1Quests.Quest), "OnDestroy")]
        // [HarmonyPostfix]
        // private static void NPCOnDestroy(S1NPCs.NPC __instance)
        // {
        //     NPCs.RemoveAll(npc => npc.S1NPC == __instance);
        //     NPC? npc = NPCs.FirstOrDefault(npc => npc.S1NPC == __instance);
        //     if (npc == null)
        //         return;
        //
        //     // npc.OnDestroyed();
        //     NPCs.Remove(npc);
        // }
    }
}
