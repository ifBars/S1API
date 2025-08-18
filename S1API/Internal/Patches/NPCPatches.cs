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

                string npcPath = Path.Combine(mainPath, customNPC.S1NPC.SaveFolderName);
                customNPC.LoadInternal(npcPath);
            }
        }

        /// <summary>
        /// Patching performed for when a single NPC starts (including modded in NPCs).
        /// </summary>
        /// <param name="__instance">Instance of the NPC</param>
        [HarmonyPatch(typeof(S1NPCs.NPC), "Start")]
        [HarmonyPostfix]
        private static void NPCStart(S1NPCs.NPC __instance) =>
            NPC.All.FirstOrDefault(npc => npc.IsCustomNPC && npc.S1NPC == __instance)?.CreateInternal();


        /// <summary>
        /// Patching performed for when an NPC calls to save data.
        /// </summary>
        /// <param name="__instance">Instance of the NPC</param>
        /// <param name="parentFolderPath">Path to the base NPC folder.</param>
        /// <param name="__result"></param>
        [HarmonyPatch(typeof(S1NPCs.NPC), "WriteData")]
        [HarmonyPostfix]
        private static void NPCWriteData(S1NPCs.NPC __instance, string parentFolderPath, ref List<string> __result) =>
            NPC.All.FirstOrDefault(npc => npc.IsCustomNPC && npc.S1NPC == __instance)?.SaveInternal(parentFolderPath, ref __result);

        /// <summary>
        /// Patching performed for when an NPC is destroyed.
        /// </summary>
        /// <param name="__instance">Instance of the NPC</param>
        [HarmonyPatch(typeof(S1NPCs.NPC), "OnDestroy")]
        [HarmonyPostfix]
        private static void NPCOnDestroy(S1NPCs.NPC __instance)
        {
            var npcToRemove = NPC.All.FirstOrDefault(npc => npc.S1NPC == __instance);
            if (npcToRemove != null)
                NPC.All.Remove(npcToRemove);
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
            var apiNpc = NPC.All.FirstOrDefault(n => n.S1NPC == s1Npc);
            if (apiNpc != null && apiNpc.IsCustomNPC)
                return false; // skip original load for custom NPCs
            return true;
        }
    }
}
