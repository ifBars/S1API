#if (IL2CPPMELON)
using S1Loaders = Il2CppScheduleOne.Persistence.Loaders;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1Map = Il2CppScheduleOne.Map;
using S1Money = Il2CppScheduleOne.Money;
using S1Economy = Il2CppScheduleOne.Economy;
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
using S1Items = Il2CppScheduleOne.ItemFramework;
using Il2CppFishNet;
using Il2CppFishNet.Object;
using Il2CppScheduleOne.DevUtilities;
using Il2CppSystem.Collections.Generic;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Loaders = ScheduleOne.Persistence.Loaders;
using S1NPCs = ScheduleOne.NPCs;
using FishNet;
using FishNet.Object;
using ScheduleOne.DevUtilities;
using S1Map = ScheduleOne.Map;
using S1Money = ScheduleOne.Money;
using S1Economy = ScheduleOne.Economy;
using S1Datas = ScheduleOne.Persistence.Datas;
using S1Items = ScheduleOne.ItemFramework;
using System.Collections.Generic;
#endif

using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using S1API.Entities;
using S1API.Entities.Relation;
using S1API.Entities.Internal;
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
                return;

            // Only the server should instantiate custom NPCs; clients will receive network spawns
            if (!InstanceFinder.IsServer)
                return;

            // Pre-scan active enterable buildings and register them for API lookup
            try
            {
                var buildings = UnityEngine.Object.FindObjectsOfType<S1Map.NPCEnterableBuilding>(includeInactive: true);
                for (int i = 0; i < buildings.Length; i++)
                {
                    var b = buildings[i];
                    if (b != null)
                        Building.Register(b);
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

                // Schedule network spawn on server via NPCNetworkBootstrap once clients are ready
                try
                {
                    var no = customNPC.gameObject.GetComponent<NetworkObject>();
                }
                catch { }
                try
                {
                    var no = customNPC.gameObject.GetComponent<NetworkObject>();
                }
                catch { }

                try
                {
                    var netObj = customNPC.gameObject.GetComponent<NetworkObject>();
                    if (netObj == null)
                    {
                        // Add a NetworkObject on server only; clients will not instantiate these directly
                        try { netObj = customNPC.gameObject.AddComponent<NetworkObject>(); } catch { }
                    }
                    if (netObj != null)
                    {
                        NPCNetworkBootstrap.RegisterPendingNetworkSpawn(customNPC, netObj, 3f, 6f);
                    }
                }
                catch { }
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
            // Apply prefab-stored identity/appearance defaults on both server and clients
            try
            {
                var identity = __instance.GetComponent<NPCPrefabIdentity>();
                if (identity != null)
                    identity.ApplyTo(__instance);
            }
            catch { }

            for (int i = 0; i < NPC.All.Count; i++)
            {
                var npc = NPC.All[i];
                if (npc.IsCustomNPC && npc.S1NPC == __instance)
                {
                    npc.CreateInternal();
                    break;
                }
            }

            // Ensure S1API per-type template prefabs are not kept in the NPCRegistry
            try
            {
                if (__instance != null && __instance.gameObject != null && __instance.gameObject.name != null)
                {
                    string n = __instance.gameObject.name;
                    if (n.StartsWith("S1API_", System.StringComparison.Ordinal))
                    {
                        var reg = S1NPCs.NPCManager.NPCRegistry;
                        for (int i = reg.Count - 1; i >= 0; i--)
                        {
                            if (reg[i] == __instance)
                            {
                                reg.RemoveAt(i);
                                break;
                            }
                        }
                    }
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

            // Skip loader entirely for S1API per-type template prefabs
            try
            {
                if (s1BaseNpc.gameObject == null)
                    return false;
                
                var go = s1BaseNpc.gameObject;
                if (go != null && (go.name == "CivilianNPC" || go.name == "BaseNPC" || go.name.StartsWith("S1API_")))
                    return false; // skip original
            }
            catch (Exception ex)
            {
                Logger.Warning($"NPCLoader_Load_Prefix: Exception in template check for ID '{baseData.ID}': {ex.Message}");
                return false;
            }

            var apiNpc = FindWrapperForS1Npc(s1BaseNpc);
            if (apiNpc == null || !apiNpc.IsCustomNPC)
                return true; // run original for base NPCs

            // Custom S1API NPC: perform safe subset of loading and skip original
            try
            {
                s1BaseNpc.Load(saveData, baseData);

                if (saveData.TryGetData("Relationship", out S1Datas.RelationshipData rel))
                {
                    if (s1BaseNpc.RelationData == null)
                    {
                        Logger.Warning($"NPCLoader_Load_Prefix: RelationData is null for '{baseData.ID}'");
                    }
                    else
                    {
                        if (!float.IsNaN(rel.RelationDelta) && !float.IsInfinity(rel.RelationDelta))
                            s1BaseNpc.RelationData.SetRelationship(rel.RelationDelta);
                        if (rel.Unlocked)
                            s1BaseNpc.RelationData.Unlock(rel.UnlockType, notify: false);
                    }
                }

                if (saveData.TryGetData("MessageConversation", out S1Datas.MSGConversationData convo))
                {
                    if (s1BaseNpc.MSGConversation == null)
                    {
                        Logger.Warning($"NPCLoader_Load_Prefix: MSGConversation is null for '{baseData.ID}'");
                    }
                    else
                    {
                        s1BaseNpc.MSGConversation.Load(convo);
                    }
                }

                if (saveData.TryGetData("CustomerData", out S1Datas.CustomerData cust))
                {
                    var customerComponent = s1BaseNpc.GetComponent<S1Economy.Customer>();

                    if (customerComponent == null)
                    {
                        Logger.Warning($"NPCLoader_Load_Prefix: Customer component is null for '{baseData.ID}'");
                    }
                    else
                    {
                        try
                        {
                            apiNpc.Customer.EnsureCustomer();
                            var npcType = apiNpc.GetType();
                            bool hasDefaults = NPC.HasCustomerDefaultsForType(npcType);
                            if (hasDefaults)
                            {
                                var defaultData = NPC.BuildCustomerDefaultsForType(npcType);
                                if (defaultData != null)
                                {
                                    bool setOk = NPC.TrySetCustomerDataOnComponent(customerComponent, defaultData);
                                    if (!setOk)
                                    {
                                        try
                                        {
#if IL2CPPMELON
                                            customerComponent.customerData = defaultData;
                                            customerComponent.currentAffinityData = defaultData.DefaultAffinityData;
#endif
                                        }
                                        catch { }
                                    }
                                }
                            }
                            customerComponent.enabled = true;
                            customerComponent.Load(cust);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning($"NPCLoader_Load_Prefix: Exception loading Customer data for '{baseData.ID}': {ex.Message}");
                            Logger.Warning($"NPCLoader_Load_Prefix: Stack trace: {ex.StackTrace}");
                        }
                    }
                }

                if (saveData.TryGetData("Inventory", out var inventoryData))
                {
                    try
                    {
                        if (S1Datas.ItemSet.TryDeserialize(inventoryData, out var itemSet))
                        {
                            itemSet.LoadTo(s1BaseNpc.Inventory.ItemSlots);
                        }
                        else
                        {
                            Logger.Warning($"Failed to deserialize inventory data for custom NPC '{baseData.ID}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"NPCLoader_Load_Prefix: Exception loading Inventory data for '{baseData.ID}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] NPCLoader.Load guard failed for custom NPC '{baseData.ID}': {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
            }

            try
            {
                var wrap = FindWrapperForS1Npc(s1BaseNpc);
                if (wrap != null)
                {
                    // Apply relationship defaults for connections even after load (connections aren't saved by base game)
                    var npcType = wrap.GetType();
                    if (NPC.TypeToRelationshipDefaults.TryGetValue(npcType, out var relCfg) && relCfg != null)
                    {
                        var builder = new NPCRelationshipDataBuilder();
                        relCfg(builder);
                        var rel = s1BaseNpc.RelationData;
                        if (rel != null)
                        {
                            // Only apply connections if the list is empty (connections aren't persisted)
                            if (rel.Connections == null || rel.Connections.Count == 0)
                            {
                                builder.ApplyTo(rel, s1BaseNpc);
                            }
                        }
                    }
                    
                    // Mark that this instance was hydrated from save data to prevent defaults overwrite
                    typeof(NPC).GetMethod("MarkLoadedFromSave", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(wrap, null);
                }
            }
            catch { }

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
                    Building.Unregister(building);
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
        /// Guard: Template prefabs created by S1API should never be saved.
        /// If the object name starts with "S1API_" treat it as a non-saveable template.
        /// </summary>
        [HarmonyPatch(typeof(S1NPCs.NPC), nameof(S1NPCs.NPC.ShouldSave))]
        [HarmonyPrefix]
        private static bool NPC_ShouldSave_Prefix(S1NPCs.NPC __instance, ref bool __result)
        {
            try
            {
                if (__instance != null && __instance.gameObject != null)
                {
                    string n = __instance.gameObject.name;
                    if (!string.IsNullOrEmpty(n) && n.StartsWith("S1API_", StringComparison.Ordinal))
                    {
                        __result = false;
                        return false; // skip original
                    }
                }
            }
            catch { }
            return true;
        }

        /// <summary>
        /// Replace NPCManager.GetSaveString with a sanitized implementation that ignores
        /// S1API template prefabs (name starts with "S1API_").
        /// Prevents NullReferenceExceptions when templates exist in NPCRegistry.
        /// </summary>
        [HarmonyPatch(typeof(S1NPCs.NPCManager), nameof(S1NPCs.NPCManager.GetSaveString))]
        [HarmonyPrefix]
        private static bool NPCManager_GetSaveString_Prefix(ref string __result)
        {
            try
            {
                var reg = S1NPCs.NPCManager.NPCRegistry;
                if (reg == null)
                {
                    __result = new S1Datas.NPCCollectionData(new S1Datas.DynamicSaveData[0]).GetJson();
                    return false;
                }

                var list = new System.Collections.Generic.List<S1Datas.DynamicSaveData>();
                for (int i = 0; i < reg.Count; i++)
                {
                    var npc = reg[i];
                    if (npc == null)
                        continue;
                    try
                    {
                        string n = npc.gameObject != null ? npc.gameObject.name : null;
                        if (!string.IsNullOrEmpty(n) && n.StartsWith("S1API_", StringComparison.Ordinal))
                            continue; // skip template prefab entries

                        if (npc.ShouldSave())
                        {
                            var data = npc.GetSaveData();
                            if (data != null)
                                list.Add(data);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"NPCManager_GetSaveString_Prefix: Skipping NPC at index {i} due to exception: {ex.Message}");
                    }
                }

                __result = new S1Datas.NPCCollectionData(list.ToArray()).GetJson();
                return false; // skip original
            }
            catch (Exception ex)
            {
                Logger.Warning($"NPCManager_GetSaveString_Prefix: Exception building save JSON: {ex.Message}");
                __result = new S1Datas.NPCCollectionData(new S1Datas.DynamicSaveData[0]).GetJson();
                return false;
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

            // Mark as loaded from save so prefab defaults won't overwrite
            try
            {
                typeof(NPC).GetMethod("MarkLoadedFromSave", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(apiNpc, null);
            }
            catch { }
        }

        /// <summary>
        /// Utility to find a base-game NPC by ID in a way compatible with both System and Il2Cpp lists.
        /// </summary>
        private static S1NPCs.NPC FindBaseNpcById(string id)
        {
            try
            {
                var reg = S1NPCs.NPCManager.NPCRegistry;
                if (reg == null)
                    return null;
                
                for (int i = 0; i < reg.Count; i++)
                {
                    var n = reg[i];
                    if (n != null)
                    {
                        if (n.ID == id)
                        {
                            return n;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Warning($"FindBaseNpcById: Exception searching for ID '{id}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Utility to find the S1API wrapper for a base-game NPC.
        /// </summary>
        private static NPC FindWrapperForS1Npc(S1NPCs.NPC baseNpc)
        {
            try
            {
                if (baseNpc == null)
                    return null;
                
                for (int i = 0; i < NPC.All.Count; i++)
                {
                    var n = NPC.All[i];
                    if (n == null)
                        continue;
                    
                    if (n.S1NPC == baseNpc)
                        return n;
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Warning($"FindWrapperForS1Npc: Exception searching for base NPC '{baseNpc?.ID}': {ex.Message}");
                return null;
            }
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
