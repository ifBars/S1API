#if (IL2CPPMELON)
using S1Loaders = Il2CppScheduleOne.Persistence.Loaders;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Loaders = ScheduleOne.Persistence.Loaders;
using S1NPCs = ScheduleOne.NPCs;
#endif

#if (IL2CPPMELON)
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Datas = ScheduleOne.Persistence.Datas;
#endif

#if (IL2CPPMELON || IL2CPPBEPINEX)
using Il2CppSystem.Collections.Generic;
#elif (MONOMELON || MONOBEPINEX)
using System.Collections.Generic;
#endif

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using S1API.Entities;
using S1API.Internal.Utils;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: All patches related to NPCs.
    /// </summary>
    [HarmonyPatch]
    internal class NPCPatches
    {
        /// <summary>
        /// Patching performed for when game NPCs are loaded.
        /// </summary>
        /// <param name="__instance">NPCsLoader</param>
        /// <param name="mainPath">Path to the base NPC folder.</param>
        [HarmonyPatch(typeof(S1Loaders.NPCsLoader), "Load")]
        [HarmonyPrefix]
        private static void NPCsLoadersLoad(S1Loaders.NPCsLoader __instance, string mainPath)
        {
            foreach (Type type in ReflectionUtils.GetDerivedClasses<NPC>())
            {
                NPC? customNPC = (NPC)Activator.CreateInstance(type, true)!;
                if (customNPC == null)
                    throw new Exception($"Unable to create instance of {type.FullName}!");

                // We skip any S1API NPCs, as they are base NPC wrappers.
                if (type.Assembly == Assembly.GetExecutingAssembly())
                    continue;

                // For old saves (NPCs folder present), load SaveableFields from per-NPC folder.
                // For new saves (NPCs.json present), let NPCLoader patch hydrate via DynamicSaveData.
                string consolidatedPath = Path.Combine(mainPath, "NPCs.json");
                if (!File.Exists(consolidatedPath))
                {
                    string npcPath = Path.Combine(mainPath, customNPC.S1NPC.SaveFolderName);
                    customNPC.LoadInternal(npcPath);
                }
            }
        }

        /// <summary>
        /// Patching performed for when a single NPC starts (including modded in NPCs).
        /// </summary>
        /// <param name="__instance">Instance of the NPC</param>
        [HarmonyPatch(typeof(S1NPCs.NPC), "Start")]
        [HarmonyPostfix]
        private static void NPCStart(S1NPCs.NPC __instance)
        {
            for (int i = 0; i < NPC.All.Count; i++)
            {
                var npc = NPC.All[i];
                if (npc.IsCustomNPC && npc.S1NPC == __instance)
                {
                    npc.CreateInternal();
                    break;
                }
            }
        }


        /// <summary>
        /// Patching performed for when an NPC calls to save data.
        /// </summary>
        /// <param name="__instance">Instance of the NPC</param>
        /// <param name="parentFolderPath">Path to the base NPC folder.</param>
        /// <param name="__result"></param>
        [HarmonyPatch(typeof(S1NPCs.NPC), "WriteData")]
        [HarmonyPostfix]
        private static void NPCWriteData(S1NPCs.NPC __instance, string parentFolderPath, ref List<string> __result)
        {
            // If consolidated NPCs.json is present, do not emit per-NPC side files for S1API saveables
            string consolidatedPath = Path.Combine(parentFolderPath, "NPCs.json");
            if (File.Exists(consolidatedPath))
                return;

            for (int i = 0; i < NPC.All.Count; i++)
            {
                var npc = NPC.All[i];
                if (npc.IsCustomNPC && npc.S1NPC == __instance)
                {
                    npc.SaveInternal(parentFolderPath, ref __result);
                    break;
                }
            }
        }

        /// <summary>
        /// Append S1API Saveable fields into the new consolidated NPCs.json via DynamicSaveData.
        /// </summary>
        /// <param name="__instance">The base NPC instance being saved.</param>
        /// <param name="__result">The dynamic save data to append to.</param>
        [HarmonyPatch(typeof(S1NPCs.NPC), nameof(S1NPCs.NPC.GetSaveData))]
        [HarmonyPostfix]
        private static void NPC_GetSaveData(S1NPCs.NPC __instance, ref S1Datas.DynamicSaveData __result)
        {
            var apiNpc = FindWrapperForS1Npc(__instance);
            if (apiNpc == null)
                return;

            apiNpc.SaveToDynamic(__result);
        }

        /// <summary>
        /// Patching performed for when an NPC is destroyed.
        /// </summary>
        /// <param name="__instance">Instance of the NPC</param>
        [HarmonyPatch(typeof(S1NPCs.NPC), "OnDestroy")]
        [HarmonyPostfix]
        private static void NPCOnDestroy(S1NPCs.NPC __instance)
        {
            for (int i = 0; i < NPC.All.Count; i++)
            {
                var npc = NPC.All[i];
                if (npc.S1NPC == __instance)
                {
                    NPC.All.Remove(npc);
                    break;
                }
            }
        }

        /// <summary>
        /// Prevent loading Health for custom NPCs during save load to avoid SyncVar initialization issues.
        /// Base NPCs continue to use the original method.
        /// </summary>
        /// <param name="__instance">NPCHealth instance</param>
        /// <param name="healthData">Saved health data</param>
        /// <returns>False to skip original for custom NPCs; true otherwise.</returns>
        [HarmonyPatch(typeof(S1NPCs.NPCHealth), "Load")]
        [HarmonyPrefix]
        private static bool NPCHealthLoad(S1NPCs.NPCHealth __instance, S1Datas.NPCHealthData healthData)
        {
            var s1Npc = __instance.GetComponent<S1NPCs.NPC>();
            var apiNpc = FindWrapperForS1Npc(s1Npc);
            if (apiNpc != null && apiNpc.IsCustomNPC)
                return false; // skip original load for custom NPCs
            return true;
        }

        /// <summary>
        /// After the base loader has applied standard data (relationship, messages, etc.),
        /// hydrate any S1API Saveable fields from DynamicSaveData into the API NPC instance.
        /// </summary>
        /// <param name="saveData">The dynamic save data for an individual NPC.</param>
        [HarmonyPatch(typeof(S1Loaders.NPCLoader), nameof(S1Loaders.NPCLoader.Load))]
        [HarmonyPostfix]
        private static void NPCLoader_Load_Postfix(S1Datas.DynamicSaveData saveData)
        {
            if (saveData == null)
                return;

            var baseData = saveData.ExtractBaseData<S1Datas.NPCData>();
            if (baseData == null || string.IsNullOrEmpty(baseData.ID))
                return;

            var s1BaseNpc = FindBaseNpcById(baseData.ID);
            if (s1BaseNpc == null)
                return;

            var apiNpc = FindWrapperForS1Npc(s1BaseNpc);
            if (apiNpc == null)
                return;

            apiNpc.LoadFromDynamic(saveData);
        }

        /// <summary>
        /// Utility to find a base-game NPC by ID in a way compatible with both System and Il2Cpp lists.
        /// </summary>
        private static S1NPCs.NPC FindBaseNpcById(string id)
        {
            var reg = S1NPCs.NPCManager.NPCRegistry;
            for (int i = 0; i < reg.Count; i++)
            {
                var n = reg[i];
                if (n != null && n.ID == id)
                    return n;
            }
            return null;
        }

        /// <summary>
        /// Utility to find the S1API wrapper for a base-game NPC.
        /// </summary>
        private static NPC FindWrapperForS1Npc(S1NPCs.NPC baseNpc)
        {
            for (int i = 0; i < NPC.All.Count; i++)
            {
                var n = NPC.All[i];
                if (n.S1NPC == baseNpc)
                    return n;
            }
            return null;
        }
    }
}
