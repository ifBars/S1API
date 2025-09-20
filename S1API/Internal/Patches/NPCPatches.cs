#if (IL2CPPMELON)
using S1Loaders = Il2CppScheduleOne.Persistence.Loaders;
using S1NPCs = Il2CppScheduleOne.NPCs;
using Il2CppFishNet;
using Il2CppScheduleOne.DevUtilities;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Loaders = ScheduleOne.Persistence.Loaders;
using S1NPCs = ScheduleOne.NPCs;
using FishNet;
using ScheduleOne.DevUtilities;
using S1Map = ScheduleOne.Map;
#endif

#if (IL2CPPMELON)
using S1Money = Il2CppScheduleOne.Money;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Money = ScheduleOne.Money;
#endif

#if (IL2CPPMELON)
using S1Economy = Il2CppScheduleOne.Economy;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Economy = ScheduleOne.Economy;
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
using System.Reflection;
using HarmonyLib;

using MelonLoader;
using S1API.Entities;
using S1API.Internal.Utils;
using S1API.Map;
using UnityEngine.SceneManagement;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: All patches related to NPCs.
    /// </summary>
    [HarmonyPatch]
    internal class NPCPatches
    {
        private static readonly Logging.Log Logger = new Logging.Log("NPCPatches");
        
        /// <summary>
        /// Patching performed for when game NPCs are loaded.
        /// </summary>
        /// <param name="__instance">NPCsLoader</param>
        /// <param name="mainPath">Path to the base NPC folder.</param>
        [HarmonyPatch(typeof(S1Loaders.NPCsLoader), "Load")]
        [HarmonyPrefix]
        private static void NPCsLoadersLoad(S1Loaders.NPCsLoader __instance, string mainPath)
        {
            // Only allow custom NPC instantiation in the "Main" scene to avoid prologue issues
            if (!IsInMainScene())
            {
                MelonLogger.Msg("[S1API] Skipping custom NPC instantiation because active scene is not 'Main'.");
                return;
            }

            // Pre-scan active enterable buildings and register them for API lookup
            try
            {
                var buildings = UnityEngine.Object.FindObjectsOfType<S1Map.NPCEnterableBuilding>(includeInactive: true);
                for (int i = 0; i < buildings.Length; i++)
                {
                    var b = buildings[i];
                    if (b != null)
                        Buildings.Register(b);
                }
            }
            catch { }
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
        /// Guard NPCInventory fields before Awake_UserLogic runs to prevent NREs on custom NPCs (Issue arises when using Breads Storage Tweaks).
        /// </summary>
        /// <param name="__instance">NPCInventory instance</param>
        [HarmonyPatch(typeof(S1NPCs.NPCInventory), "Awake")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void EnsureNPCInventorySafeInit(S1NPCs.NPCInventory __instance)
        {
            // Ensure definition arrays are not null before length/enumeration in Awake_UserLogic
#if (IL2CPPMELON || IL2CPPBEPINEX)
            if (__instance.TestItems == null)
                __instance.TestItems = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppScheduleOne.ItemFramework.ItemDefinition>(0);
            if (__instance.StartupItems == null)
                __instance.StartupItems = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppScheduleOne.ItemFramework.ItemDefinition>(0);
            if (__instance.RandomItemDefinitions == null)
                __instance.RandomItemDefinitions = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppScheduleOne.ItemFramework.StorableItemDefinition>(0);
#else
            if (__instance.TestItems == null)
                __instance.TestItems = Array.Empty<ScheduleOne.ItemFramework.ItemDefinition>();
            if (__instance.StartupItems == null)
                __instance.StartupItems = Array.Empty<ScheduleOne.ItemFramework.ItemDefinition>();
            if (__instance.RandomItemDefinitions == null)
                __instance.RandomItemDefinitions = Array.Empty<ScheduleOne.ItemFramework.StorableItemDefinition>();
#endif
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

            // Register building if this NPC is inside an enterable building component hierarchy
            try
            {
                var building = __instance.GetComponentInParent<S1Map.NPCEnterableBuilding>(true);
                if (building != null)
                {
                    Buildings.Register(building);
                }
            }
            catch { }
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
        /// Temporary patch while S1API NPCs are not networked
        /// Handle NPCLoader.Load for custom S1API NPCs to avoid inventory hydration which uses networking.
        /// Replicates core parts of the original loader except Inventory and Health (Health already guarded).
        /// </summary>
        [HarmonyPatch(typeof(S1Loaders.NPCLoader), nameof(S1Loaders.NPCLoader.Load))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool NPCLoader_Load_Prefix(S1Datas.DynamicSaveData saveData)
        {
            // Do not intercept on non-Main scenes; let base loader run unmodified
            if (!IsInMainScene())
                return true;
            if (saveData == null)
                return true;

            var baseData = saveData.ExtractBaseData<S1Datas.NPCData>();
            if (baseData == null || string.IsNullOrEmpty(baseData.ID))
                return true;

            var s1BaseNpc = FindBaseNpcById(baseData.ID);
            if (s1BaseNpc == null)
                return true;

            // Skip loader entirely for the CustomerNPC template prefab
            try
            {
                var go = s1BaseNpc.gameObject;
                if (go != null && go.name == "CustomerNPC")
                {
                    // Ignore creation/processing for the template clone we registered as spawnable
                    return false; // skip original
                }
            }
            catch { }

            var apiNpc = FindWrapperForS1Npc(s1BaseNpc);
            if (apiNpc == null || !apiNpc.IsCustomNPC)
                return true; // run original for base NPCs

            // Custom S1API NPC: perform safe subset of loading and skip original
            try
            {
                s1BaseNpc.Load(saveData, baseData);

                if (saveData.TryGetData("Relationship", out S1Datas.RelationshipData rel))
                {
                    if (!float.IsNaN(rel.RelationDelta) && !float.IsInfinity(rel.RelationDelta))
                        s1BaseNpc.RelationData.SetRelationship(rel.RelationDelta);
                    if (rel.Unlocked)
                        s1BaseNpc.RelationData.Unlock(rel.UnlockType, notify: false);
                }

                if (saveData.TryGetData("MessageConversation", out S1Datas.MSGConversationData convo))
                {
                    s1BaseNpc.MSGConversation.Load(convo);
                }

                if (saveData.TryGetData("CustomerData", out S1Datas.CustomerData cust) && s1BaseNpc.GetComponent<S1Economy.Customer>() != null)
                {
                    s1BaseNpc.GetComponent<S1Economy.Customer>().Load(cust);
                }

                // Intentionally skip Inventory hydration for custom NPCs
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[S1API] NPCLoader.Load guard failed for custom NPC: {ex.Message}");
            }

            return false; // skip original
        }

        /// <summary>
        /// Patching performed for when an NPC is destroyed.
        /// </summary>
        /// <param name="__instance">Instance of the NPC</param>
        [HarmonyPatch(typeof(S1NPCs.NPC), "OnDestroy")]
        [HarmonyPostfix]
        private static void NPCOnDestroy(S1NPCs.NPC __instance)
        {
            // Unregister any building tied to this NPC's parent chain if applicable
            try
            {
                var building = __instance.GetComponentInParent<S1Map.NPCEnterableBuilding>(true);
                if (building != null)
                {
                    Buildings.Unregister(building);
                }
            }
            catch { }

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
        /// Temporary patch while S1API NPCs are not networked
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
        /// Temporary patch while S1API NPCs are not networked
        /// Skip networking in NPCInventory.OnSleepStart for custom NPCs and perform local-only updates.
        /// Prevents RPC paths which require FishNet initialization.
        /// </summary>
        [HarmonyPatch(typeof(S1NPCs.NPCInventory), "OnSleepStart")]
        [HarmonyPrefix]
        private static bool NPCInventory_OnSleepStart_Prefix(S1NPCs.NPCInventory __instance)
        {
            if (!IsInMainScene())
                return true; // run original outside of Main
            var baseNpc = __instance.GetComponent<S1NPCs.NPC>();
            var apiNpc = baseNpc != null ? FindWrapperForS1Npc(baseNpc) : null;
            if (apiNpc == null || !apiNpc.IsCustomNPC)
                return true; // run original for base NPCs

            if (!InstanceFinder.IsServer)
                return false; // mirror original early-return for clients

            try
            {
                // Clear inventory without networking
                if (__instance.ClearInventoryEachNight)
                {
                    for (int i = 0; i < __instance.ItemSlots.Count; i++)
                    {
                        var slot = __instance.ItemSlots[i];
                        if (slot != null)
                            slot.ClearStoredInstance(true);
                    }
                }

                // Respect minimal fill
                if (__instance.GetItemCount() >= 3)
                    return false;

                // Random cash (local-only)
                if (__instance.RandomCash)
                {
                    int amount = UnityEngine.Random.Range(__instance.RandomCashMin, __instance.RandomCashMax);
                    if (amount > 0)
                    {
                        var cash = NetworkSingleton<S1Money.MoneyManager>.Instance.GetCashInstance(amount);
                        __instance.InsertItem(cash, network: false);
                    }
                }

                // Random items (local-only)
                if (__instance.RandomItems && __instance.RandomItemDefinitions != null && __instance.RandomItemDefinitions.Length > 0)
                {
                    int count = UnityEngine.Random.Range(__instance.RandomItemMin, __instance.RandomItemMax + 1);
                    for (int i = 0; i < count; i++)
                    {
                        var def = __instance.RandomItemDefinitions[UnityEngine.Random.Range(0, __instance.RandomItemDefinitions.Length)];
                        var inst = def.GetDefaultInstance();
                        __instance.InsertItem(inst, network: false);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[S1API] NPCInventory.OnSleepStart guard failed for custom NPC: {ex.Message}");
            }

            // Skip original method for custom NPCs
            return false;
        }

        /// <summary>
        /// Temporary patch while S1API NPCs are not networked
        /// Guard Revive() for custom S1API NPCs to avoid Health SyncVar access before FishNet init.
        /// Applies equivalent revive effects without touching the SyncVar setter path.
        /// </summary>
        [HarmonyPatch(typeof(S1NPCs.NPCHealth), nameof(S1NPCs.NPCHealth.Revive))]
        [HarmonyPrefix]
        private static bool NPCHealth_Revive_Prefix(S1NPCs.NPCHealth __instance)
        {
            if (!IsInMainScene())
                return true; // allow original behaviour outside of Main
            var baseNpc = __instance.GetComponent<S1NPCs.NPC>();
            var apiNpc = baseNpc != null ? FindWrapperForS1Npc(baseNpc) : null;
            if (apiNpc == null || !apiNpc.IsCustomNPC)
                return true; // use original for base NPCs

            // Skip S1API NPCs for now
            return false;
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
            if (!IsInMainScene())
                return;
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

        /// <summary>
        /// Returns true only when the active scene is exactly "Main" (case-insensitive).
        /// Custom NPC flow is restricted to this scene to avoid prologue issues.
        /// </summary>
        private static bool IsInMainScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return string.Equals(sceneName, "Main", StringComparison.OrdinalIgnoreCase);
        }
    }
}
