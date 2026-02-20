#if (IL2CPPMELON)
using S1Relation = Il2CppScheduleOne.NPCs.Relation;
using S1Loaders = Il2CppScheduleOne.Persistence.Loaders;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsBehaviour = Il2CppScheduleOne.NPCs.Behaviour;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1Map = Il2CppScheduleOne.Map;
using S1Money = Il2CppScheduleOne.Money;
using S1Economy = Il2CppScheduleOne.Economy;
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
using S1Items = Il2CppScheduleOne.ItemFramework;
using S1GameTime = Il2CppScheduleOne.GameTime;
using S1Quests = Il2CppScheduleOne.Quests;
using Il2CppFishNet;
using Il2CppFishNet.Object;
using Il2CppScheduleOne.DevUtilities;
using Il2CppSystem.Collections.Generic;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Relation = ScheduleOne.NPCs.Relation;
using S1Loaders = ScheduleOne.Persistence.Loaders;
using S1NPCs = ScheduleOne.NPCs;
using S1NPCsBehaviour = ScheduleOne.NPCs.Behaviour;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using FishNet;
using FishNet.Object;
using ScheduleOne.DevUtilities;
using S1Map = ScheduleOne.Map;
using S1Money = ScheduleOne.Money;
using S1Economy = ScheduleOne.Economy;
using S1Datas = ScheduleOne.Persistence.Datas;
using S1Items = ScheduleOne.ItemFramework;
using S1GameTime = ScheduleOne.GameTime;
using S1Quests = ScheduleOne.Quests;
using System.Collections.Generic;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using S1API.Entities;
using S1API.Entities.Relation;
using S1API.Internal.Entities;
using S1API.Internal.Lifecycle;
using S1API.Internal.Utils;
using S1API.Map;
using UnityEngine;
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
        private static readonly HashSet<string> _loadingDealers = new HashSet<string>();
        public static bool CustomNpcsReady = false;
        // Pending custom NPC types to instantiate when using consolidated NPCs.json saves (non-physical/custom contacts).
        private static readonly System.Collections.Generic.List<Type> _pendingCustomNpcTypes = new System.Collections.Generic.List<Type>();
        // Base-game additional save keys. If a DynamicSaveData has anything outside this set, we treat it as custom.
        private static readonly HashSet<string> BaseNpcAdditionalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Relationship", "Inventory", "CustomerData", "MessageConversation", "Health"
        };

        // Dictionary to track Customer instances with saved CurrentAddiction values that should not be reset by Awake
        private static readonly System.Collections.Generic.Dictionary<S1Economy.Customer, float> _savedCurrentAddiction
            = new System.Collections.Generic.Dictionary<S1Economy.Customer, float>();

        /// <summary>
        /// Comparison function for sorting NPCAction by StartTime, with signals coming before non-signals when times are equal.
        /// </summary>
        private static int CompareNPCActions(S1NPCsSchedules.NPCAction a, S1NPCsSchedules.NPCAction b)
        {
            int timeComparison = a.StartTime.CompareTo(b.StartTime);
            if (timeComparison != 0)
            {
                return timeComparison;
            }
            // When StartTime is equal, signals come before non-signals
            // Both signals or both non-signals = equal (return 0)
            if (a.IsSignal == b.IsSignal)
            {
                return 0;
            }
            return a.IsSignal ? -1 : 1; // Signal comes before non-signal
        }

        private static bool HasConsolidatedSave(string mainPath)
        {
            string parentSaveFolder = Directory.GetParent(mainPath)?.FullName ?? mainPath;
            string consolidatedPath = Path.Combine(parentSaveFolder, "NPCs.json");
            return File.Exists(consolidatedPath);
        }

        /// <summary>
        /// Queue up custom NPC types so we can instantiate them in save order when using NPCs.json.
        /// </summary>
        private static void RebuildPendingCustomNpcTypes(bool useConsolidatedFlow)
        {
            _pendingCustomNpcTypes.Clear();
            if (!useConsolidatedFlow)
                return;

            var types = ReflectionUtils.GetDerivedClasses<NPC>();
            // Deterministic ordering to keep static-indexed mods stable (e.g., Empire NPC1..NPC50)
            types.Sort((a, b) => string.Compare(a?.Name, b?.Name, StringComparison.Ordinal));

            foreach (Type type in types)
            {
                if (type == null || type.IsAbstract)
                    continue;
                if (type.Assembly == Assembly.GetExecutingAssembly())
                    continue; // skip S1API internal wrapper types

                _pendingCustomNpcTypes.Add(type);
            }
        }

        /// <summary>
        /// Heuristic to determine if a DynamicSaveData belongs to a custom NPC by looking for non-base additional data keys.
        /// </summary>
        private static bool IsLikelyCustomDynamicSaveData(S1Datas.DynamicSaveData saveData)
        {
            if (saveData == null)
                return false;

            try
            {
                var extras = saveData.AdditionalDatas;
                if (extras == null)
                    return false;

                int count = extras.Count;
                for (int i = 0; i < count; i++)
                {
                    var data = extras[i];
                    if (data == null)
                        continue;

                    string name = data.Name;
                    if (string.IsNullOrEmpty(name))
                        continue;

                    if (!BaseNpcAdditionalKeys.Contains(name))
                        return true;
                }
            }
            catch
            {
                // Swallow and treat as non-custom
            }

            return false;
        }

        private static void RegisterCustomNpcForNetworking(NPC customNPC)
        {
            if (customNPC == null || customNPC.gameObject == null)
                return;

            try
            {
                var netObj = customNPC.gameObject.GetComponent<NetworkObject>();
                if (netObj == null)
                {
                    try
                    {
                        netObj = customNPC.gameObject.AddComponent<NetworkObject>();
                    }
                    catch
                    {
                    }
                }

                if (netObj != null)
                {
                    NPCNetworkBootstrap.RegisterPendingNetworkSpawn(customNPC, netObj, 3f, 6f);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Instantiate a pending custom NPC whose default ID matches the save ID (case-insensitive).
        /// Returns the instance if successful; otherwise null. Non-matching temporary instances
        /// are cleaned up immediately to avoid registration side effects.
        /// </summary>
        private static NPC TryInstantiateCustomNpcForSaveId(string npcId, string reasonTag)
        {
            if (_pendingCustomNpcTypes.Count == 0)
                return null;

            // First check if NPC already exists - don't create duplicates
            var existingNpc = FindBaseNpcById(npcId);
            if (existingNpc != null)
            {
                // NPC already exists, find the wrapper
                var wrapper = FindWrapperForS1Npc(existingNpc);
                if (wrapper != null)
                {
                    // Remove the type from pending list since this NPC is already instantiated
                    // Find the matching type and remove it
                    for (int i = _pendingCustomNpcTypes.Count - 1; i >= 0; i--)
                    {
                        var type = _pendingCustomNpcTypes[i];
                        if (type != null && wrapper.GetType() == type)
                        {
                            _pendingCustomNpcTypes.RemoveAt(i);
                            break;
                        }
                    }
                    return wrapper;
                }
                return null; // NPC exists but no wrapper found - shouldn't happen
            }

            for (int i = 0; i < _pendingCustomNpcTypes.Count; i++)
            {
                var type = _pendingCustomNpcTypes[i];
                NPC? customNPC = null;
                try
                {
                    customNPC = (NPC)Activator.CreateInstance(type, true)!;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to instantiate custom NPC type '{type?.FullName ?? "<null>"}' for save ID '{npcId}': {ex.Message}");
                }

                if (customNPC == null)
                    continue;

                string defaultId = null;
                try { defaultId = customNPC.S1NPC?.ID; } catch { }

                bool idMatches = !string.IsNullOrEmpty(npcId)
                                 && !string.IsNullOrEmpty(defaultId)
                                 && defaultId.Equals(npcId, StringComparison.OrdinalIgnoreCase);

                if (idMatches)
                {
                    _pendingCustomNpcTypes.RemoveAt(i);
                    RegisterCustomNpcForNetworking(customNPC);
                    return customNPC;
                }

                // Cleanup non-matching instance to avoid registry pollution
                try { NPC.All.Remove(customNPC); } catch { }
                try { UnityEngine.Object.DestroyImmediate(customNPC.gameObject); } catch { }
            }

            return null;
        }

        /// <summary>
        /// Instantiate any remaining custom NPC types that were not present in the save (newly added NPCs).
        /// </summary>
        private static void InstantiateRemainingCustomNpcs(string mainPath)
        {
            if (_pendingCustomNpcTypes.Count == 0)
                return;

            while (_pendingCustomNpcTypes.Count > 0)
            {
                Type type = _pendingCustomNpcTypes[0];
                _pendingCustomNpcTypes.RemoveAt(0);
                NPC? customNPC = null;
                try
                {
                    customNPC = (NPC)Activator.CreateInstance(type, true)!;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to instantiate custom NPC type '{type?.FullName ?? "<null>"}' with default data: {ex.Message}");
                }

                if (customNPC == null)
                    continue;

                // Initialize default state from legacy per-NPC folder (if it exists) to give new NPCs sensible defaults.
                try
                {
                    string npcPath = Path.Combine(mainPath, customNPC.S1NPC.SaveFolderName);
                    customNPC.LoadInternal(npcPath);
                }
                catch
                {
                }

                RegisterCustomNpcForNetworking(customNPC);
            }
        }

        /// <summary>
        /// Patching performed for when game NPCs are loaded.
        /// Creates custom NPC instances before the loader runs.
        /// This MUST run first (before loading) so NPCs exist when NPCLoader tries to find them.
        /// </summary>
        /// <param name="__instance">NPCsLoader</param>
        /// <param name="mainPath">Path to the base NPC folder.</param>
        [HarmonyPatch(typeof(S1Loaders.NPCsLoader), "Load")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void NPCsLoadersLoad(S1Loaders.NPCsLoader __instance, string mainPath)
        {
            // Build pending list (used later for consolidated flow as a fallback)
            bool hasConsolidatedSave = HasConsolidatedSave(mainPath);
            RebuildPendingCustomNpcTypes(hasConsolidatedSave);

            // Only allow custom NPC instantiation in the "Main" scene to avoid prologue issues
            if (!IsInMainScene())
                return;

            // Only the server should instantiate custom NPCs; clients will receive network spawns
            if (!InstanceFinder.IsServer)
                return;

            // Check if consolidated save (NPCs.json) exists
            string parentSaveFolder = Directory.GetParent(mainPath)?.FullName ?? mainPath;
            string consolidatedPath = Path.Combine(parentSaveFolder, "NPCs.json");
            bool hasConsolidatedFile = File.Exists(consolidatedPath);

            // If NPCs.json exists, don't create NPCs upfront - let NPCsLoader_Load_Prefix handle instantiation
            // This prevents duplicate creation when loading from consolidated save
            if (hasConsolidatedFile)
            {
                // Don't clear pending list - NPCsLoader_Load_Prefix will use it to instantiate NPCs as needed
                return;
            }

            // Legacy save format: create all NPCs upfront and load from per-NPC folders
            int createdCount = 0;
            foreach (Type type in ReflectionUtils.GetDerivedClasses<NPC>())
            {
                if (type.IsAbstract)
                    continue;
                
                NPC? customNPC = (NPC)Activator.CreateInstance(type, true)!;
                if (customNPC == null)
                    throw new Exception($"Unable to create instance of {type.FullName}!");

                // We skip any S1API NPCs, as they are base NPC wrappers.
                if (type.Assembly == Assembly.GetExecutingAssembly())
                    continue;

                string npcId = customNPC.S1NPC?.ID ?? "<null>";

                // Load SaveableFields from per-NPC folder for legacy saves
                string npcPath = Path.Combine(mainPath, customNPC.S1NPC.SaveFolderName);
                customNPC.LoadInternal(npcPath);

                try
                {
                    var netObj = customNPC.gameObject.GetComponent<NetworkObject>();
                    if (netObj == null)
                    {
                        try
                        {
                            netObj = customNPC.gameObject.AddComponent<NetworkObject>();
                        }
                        catch
                        {
                        }
                    }

                    if (netObj != null)
                    {
                        NPCNetworkBootstrap.RegisterPendingNetworkSpawn(customNPC, netObj, 3f, 6f);
                    }
                }
                catch
                {
                }
                
                createdCount++;
            }

            // We instantiated all custom NPCs up front; clear pending list to avoid duplicate fallback instantiation.
            _pendingCustomNpcTypes.Clear();
            CustomNpcsReady = true;
        }

        /// <summary>
        /// Patch NPCsLoader.Load to check parent directory for NPCs.json since it's stored in the save root, not the NPCs subfolder.
        /// This runs AFTER NPCsLoadersLoad so custom NPCs exist when we try to load them.
        /// </summary>
        [HarmonyPatch(typeof(S1Loaders.NPCsLoader), "Load")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Normal)]
        private static bool NPCsLoader_Load_Prefix(S1Loaders.NPCsLoader __instance, string mainPath)
        {
            // Check parent directory for NPCs.json (it's stored in save root, not NPCs subfolder)
            string parentSaveFolder = Directory.GetParent(mainPath)?.FullName ?? mainPath;
            string npcsJsonPath = Path.Combine(parentSaveFolder, "NPCs.json");
            
            if (File.Exists(npcsJsonPath))
            {
                // Load from parent directory instead
                try
                {
                    string contents = File.ReadAllText(npcsJsonPath);
                    var nPCCollectionData = JsonUtility.FromJson<S1Datas.NPCCollectionData>(contents);
                    if (nPCCollectionData != null)
                    {
                        S1Loaders.NPCLoader nPCLoader = new S1Loaders.NPCLoader();
                        S1Datas.DynamicSaveData[] nPCs = nPCCollectionData.NPCs;
                        if (_pendingCustomNpcTypes.Count == 0)
                            RebuildPendingCustomNpcTypes(true);
                        
                        int successCount = 0;
                        int failureCount = 0;
                        foreach (S1Datas.DynamicSaveData dynamicSaveData in nPCs)
                        {
                            if (dynamicSaveData != null)
                            {
                                try
                                {
                                    var baseData = dynamicSaveData.ExtractBaseData<S1Datas.NPCData>();
                                    // Check if NPC already exists first - don't create duplicates
                                    var existingNpc = FindBaseNpcById(baseData?.ID ?? string.Empty);
                                    
                                    // If NPC doesn't exist yet, try to instantiate it from pending types
                                    // TryInstantiateCustomNpcForSaveId will check for existing NPCs internally to prevent duplicates
                                    if (existingNpc == null && _pendingCustomNpcTypes.Count > 0)
                                    {
                                        var instantiated = TryInstantiateCustomNpcForSaveId(baseData?.ID, "no-base-npc");
                                        if (instantiated != null)
                                        {
                                            // Re-check after instantiation to get the base NPC
                                            existingNpc = FindBaseNpcById(baseData?.ID ?? string.Empty);
                                        }
                                    }
                                    
                                    // If still not found and data looks custom, try again
                                    if (existingNpc == null && IsLikelyCustomDynamicSaveData(dynamicSaveData) && _pendingCustomNpcTypes.Count > 0)
                                    {
                                        var instantiated = TryInstantiateCustomNpcForSaveId(baseData?.ID, "custom-data");
                                        if (instantiated != null)
                                        {
                                            // Re-check after instantiation to get the base NPC
                                            existingNpc = FindBaseNpcById(baseData?.ID ?? string.Empty);
                                        }
                                    }
                                    
                                    // Only load if NPC exists (either was already there or was just instantiated)
                                    // If NPC still doesn't exist, it's likely a base game NPC or a missing mod
                                    if (existingNpc != null)
                                    {
                                        nPCLoader.Load(dynamicSaveData);
                                        successCount++;
                                    }
                                }
                                catch (Exception npcEx)
                                {
                                    failureCount++;
                                    var baseData = dynamicSaveData.ExtractBaseData<S1Datas.NPCData>();
                                    string npcId = baseData?.ID ?? "<unknown>";
                                    Logger.Warning($"[S1API] NPCsLoader_Load_Prefix: Failed to load NPC '{npcId}': {npcEx.Message}");
                                    // Continue loading other NPCs even if one fails
                                }
                            }
                        }

                        // Instantiate any new custom NPCs that don't have save entries yet (e.g., newly added mods)
                        InstantiateRemainingCustomNpcs(mainPath);
                        return false; // Skip original loader
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] NPCsLoader_Load_Prefix: Error loading NPCs.json from parent directory: {ex.Message}");
                    Logger.Warning($"[S1API] NPCsLoader_Load_Prefix: Stack trace: {ex.StackTrace}");
                }
            }
            
            // Fall through to original loader (will check mainPath and handle legacy saves)
            return true;
        }

        /// <summary>
        /// Guard NPCInventory fields before Awake_UserLogic runs to prevent NREs on custom NPCs (Issue arises when using Breads Storage Tweaks).
        /// Also clears StartupItems if they were already processed/inserted by ApplyRandomInventoryDefaults to prevent duplicate insertion.
        /// </summary>
        /// <param name="__instance">NPCInventory instance</param>
        [HarmonyPatch(typeof(S1NPCs.NPCInventory), "Awake")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void EnsureNPCInventorySafeInit(S1NPCs.NPCInventory __instance)
        {
            string npcId = "<unknown>";
            bool isCustomNpc = false;
            try
            {
                var baseNpcCheck = __instance.GetComponent<S1NPCs.NPC>();
                npcId = baseNpcCheck?.ID ?? __instance.name ?? "<unknown>";
                var apiNpcCheck = baseNpcCheck != null ? FindWrapperForS1Npc(baseNpcCheck) : null;
                isCustomNpc = apiNpcCheck != null && apiNpcCheck.IsCustomNPC;
            }
            catch { }

            // Ensure definition arrays are not null before length/enumeration in Awake_UserLogic
#if (IL2CPPMELON || IL2CPPBEPINEX)
            if (__instance.TestItems == null)
                __instance.TestItems =
 new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppScheduleOne.ItemFramework.ItemDefinition>(0);
            if (__instance.StartupItems == null)
                __instance.StartupItems =
 new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppScheduleOne.ItemFramework.ItemDefinition>(0);
            if (__instance.RandomInventoryItems == null)
                __instance.RandomInventoryItems =
 new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppScheduleOne.NPCs.NPCInventory.RandomInventoryItem>(0);
#else
            if (__instance.TestItems == null)
                __instance.TestItems = Array.Empty<ScheduleOne.ItemFramework.ItemDefinition>();
            if (__instance.StartupItems == null)
                __instance.StartupItems = Array.Empty<ScheduleOne.ItemFramework.ItemDefinition>();
            if (__instance.RandomInventoryItems == null)
                __instance.RandomInventoryItems = Array.Empty<ScheduleOne.NPCs.NPCInventory.RandomInventoryItem>();
#endif

            // For custom NPCs, clear StartupItems if they're empty/null to prevent Awake from processing stale data
            // ApplyRandomInventoryDefaults() handles inserting items directly and clearing StartupItems,
            // but this provides a safety net in case Awake runs before ApplyRandomInventoryDefaults()
            try
            {
                var baseNpc = __instance.GetComponent<S1NPCs.NPC>();
                var apiNpc = baseNpc != null ? FindWrapperForS1Npc(baseNpc) : null;
                
                // Only check for custom NPCs
                if (apiNpc != null && apiNpc.IsCustomNPC)
                {
                    // If StartupItems are set but items already exist in inventory, clear StartupItems
                    // This handles the case where ApplyRandomInventoryDefaults() inserted items before Awake ran
                    if (__instance.StartupItems != null && __instance.ItemSlots != null)
                    {
                        bool hasItems = false;
                        try
                        {
                            for (int i = 0; i < __instance.ItemSlots.Count; i++)
                            {
                                var slot = __instance.ItemSlots[i];
                                if (slot != null && slot.ItemInstance != null && slot.Quantity > 0)
                                {
                                    hasItems = true;
                                    break;
                                }
                            }
                        }
                        catch { }

                        if (hasItems)
                        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                            var il2cppArray = __instance.StartupItems as Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<S1Items.ItemDefinition>;
                            if (il2cppArray != null && il2cppArray.Length > 0)
                            {
                                // Items already exist, clear StartupItems to prevent duplicate insertion
                                __instance.StartupItems = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<S1Items.ItemDefinition>(0);
                            }
#else
                            var array = __instance.StartupItems as S1Items.ItemDefinition[];
                            if (array != null && array.Length > 0)
                            {
                                // Items already exist, clear StartupItems to prevent duplicate insertion
                                __instance.StartupItems = Array.Empty<S1Items.ItemDefinition>();
                            }
#endif
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If check fails, continue normally
                Logger.Warning($"Failed to check for already-inserted startup items: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up duplicate item slots created by the base game's Awake for custom NPCs.
        /// This prevents item duplication by ensuring slots match the expected SlotCount.
        /// Also clears StartupItems after processing to prevent duplicate item insertion on subsequent Awake calls.
        /// </summary>
        /// <param name="__instance">NPCInventory instance</param>
        [HarmonyPatch(typeof(S1NPCs.NPCInventory), "Awake")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void NPCInventory_Awake_Postfix(S1NPCs.NPCInventory __instance)
        {
            if (!IsInMainScene())
                return;

            try
            {
                var baseNpc = __instance.GetComponent<S1NPCs.NPC>();
                var apiNpc = baseNpc != null ? FindWrapperForS1Npc(baseNpc) : null;
                
                // Only clean up for custom NPCs
                if (apiNpc == null || !apiNpc.IsCustomNPC)
                    return;

                // Use the wrapper's EnsureInitialized to handle slot deduplication and proper initialization
                // This centralizes slot management and prevents duplicates
                try
                {
                    var wrapperInventory = new NPCInventory(apiNpc);
                    wrapperInventory.EnsureInitialized();
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to ensure inventory initialized in postfix: {ex.Message}");
                }

                // Clear StartupItems after processing to prevent duplicate insertion on subsequent Awake calls
                // This ensures startup items are only inserted once, even if Awake runs multiple times
                if (__instance.StartupItems != null)
                {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                    var il2cppArray = __instance.StartupItems as Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<S1Items.ItemDefinition>;
                    if (il2cppArray != null && il2cppArray.Length > 0)
                    {
                        __instance.StartupItems = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<S1Items.ItemDefinition>(0);
                    }
#else
                    var array = __instance.StartupItems as S1Items.ItemDefinition[];
                    if (array != null && array.Length > 0)
                    {
                        __instance.StartupItems = Array.Empty<S1Items.ItemDefinition>();
                    }
#endif
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[S1API] NPCInventory.Awake postfix cleanup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh behaviourStack from GetComponentsInChildren so dynamically added behaviours (e.g. SmokeBreakBehaviour,
        /// GraffitiBehaviour from NPCPrefabBuilder) are included. Also ensures each Behaviour has beh and Npc set
        /// (Enable_Server requires beh; prefab build may run before Awake so these can be null on spawn).
        /// </summary>
        [HarmonyPatch(typeof(S1NPCsBehaviour.NPCBehaviour), "Start")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void NPCBehaviour_Start_Prefix(S1NPCsBehaviour.NPCBehaviour __instance)
        {
            try
            {
                var behaviours = __instance.GetComponentsInChildren<S1NPCsBehaviour.Behaviour>(true);
#if (IL2CPPMELON || IL2CPPBEPINEX)
                var list = new Il2CppSystem.Collections.Generic.List<S1NPCsBehaviour.Behaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                    list.Add(behaviours[i]);
#else
                var list = new System.Collections.Generic.List<S1NPCsBehaviour.Behaviour>(behaviours);
#endif
                ReflectionUtils.TrySetFieldOrProperty(__instance, "behaviourStack", list);
                __instance.SortBehaviourStack();

                var npc = __instance.Npc;
                if (npc == null)
                    npc = __instance.GetComponentInParent<S1NPCs.NPC>(true);
                if (npc != null)
                    ReflectionUtils.TrySetFieldOrProperty(__instance, "Npc", npc);

                for (int i = 0; i < list.Count; i++)
                {
                    var b = list[i];
                    if (b == null) continue;
                    var existingBeh = ReflectionUtils.TryGetFieldOrProperty(b, "beh");
                    if (existingBeh == null)
                        ReflectionUtils.TrySetFieldOrProperty(b, "beh", __instance);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"NPCBehaviour_Start_Prefix: Failed to refresh behaviourStack: {ex.Message}");
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
            // Apply prefab-stored identity/appearance defaults on both server and clients
            try
            {
                var identity = __instance.GetComponent<NPCPrefabIdentity>();
                if (identity != null)
                    identity.ApplyTo(__instance);
            }
            catch
            {
            }

            NPC? apiNpc = null;
            for (int i = 0; i < NPC.All.Count; i++)
            {
                var npc = NPC.All[i];
                if (npc.IsCustomNPC && npc.S1NPC == __instance)
                {
                    apiNpc = npc;
                    break;
                }
            }

            // On clients, create wrapper if it doesn't exist (network-spawned NPCs)
            if (apiNpc == null && !InstanceFinder.IsServer)
                apiNpc = NPC.CreateWrapperForNetworkSpawnedNPC(__instance);

            if (apiNpc != null && apiNpc.IsCustomNPC)
            {
                // Ensure conversation exists before CreateInternal() tries to access it
                apiNpc.EnsureMessageConversationReady(resetDefaults: false);
                apiNpc.CreateInternal();
                
                // Ensure visibility is set correctly on clients based on IsPhysical
                // On server, this is handled in FinalizeNetworkSpawn(), but clients need it here
                // We use a coroutine to delay until after the NPC is fully spawned/initialized
                if (!InstanceFinder.IsServer)
                {
                    MelonCoroutines.Start(SetClientVisibilityDelayed(__instance, apiNpc.IsPhysical));
                }
            }

            // Ensure S1API per-type template prefabs are not kept in the NPCRegistry
            try
            {
                if (__instance != null && __instance.gameObject != null && __instance.gameObject.name != null)
                {
                    string n = __instance.gameObject.name;
                    bool isTemplatePrefab = !__instance.gameObject.activeInHierarchy || !__instance.enabled;
                    if (isTemplatePrefab && n.StartsWith("S1API_", System.StringComparison.Ordinal))
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
            catch
            {
            }
        }

        /// <summary>
        /// Coroutine to set NPC visibility on clients after a delay to ensure the NPC is fully spawned/initialized.
        /// </summary>
        private static IEnumerator SetClientVisibilityDelayed(S1NPCs.NPC npc, bool isPhysical)
        {
            // Wait a frame to ensure the NPC is fully initialized and spawned
            yield return null;
            
            // Additional small delay to ensure network spawn is complete
            yield return new WaitForSeconds(0.1f);
            
            try
            {
                if (npc != null && npc.gameObject != null)
                {
                    // Set visibility directly on clients (not networked since we're a client)
                    npc.SetVisible(isPhysical, networked: false);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to set visibility for NPC '{npc?.ID}' on client: {ex.Message}");
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
        private static void NPCWriteData(S1NPCs.NPC __instance, string parentFolderPath, 
#if (IL2CPPMELON || IL2CPPBEPINEX)
            ref Il2CppSystem.Collections.Generic.List<string> __result)
#else
            ref System.Collections.Generic.List<string> __result)
#endif
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
        /// Guard GetSaveData to prevent NullReferenceException when GameObject is null or destroyed.
        /// Also prevents saving template prefabs.
        /// </summary>
        /// <param name="__instance">The base NPC instance being saved.</param>
        /// <param name="__result">The dynamic save data result.</param>
        [HarmonyPatch(typeof(S1NPCs.NPC), nameof(S1NPCs.NPC.GetSaveData))]
        [HarmonyPrefix]
        private static bool NPC_GetSaveData_Prefix(S1NPCs.NPC __instance, ref S1Datas.DynamicSaveData __result)
        {
            // Check if instance is null
            if (__instance == null)
            {
                __result = null;
                return false; // Skip original
            }

            // Check if GameObject is null or destroyed - this causes NullReferenceException in GetComponent calls
            if (__instance.gameObject == null)
            {
                Logger.Warning($"[S1API] NPC_GetSaveData_Prefix: NPC '{__instance?.ID ?? "<null>"}' has null GameObject - skipping save");
                __result = null;
                return false; // Skip original
            }

            // Check if this is a template prefab (should not be saved)
            try
            {
                string name = __instance.gameObject.name;
                if (!string.IsNullOrEmpty(name) && name.StartsWith("S1API_", StringComparison.Ordinal))
                {
                    // Template prefab - don't save
                    __result = null;
                    return false; // Skip original
                }
            }
            catch
            {
                // If we can't check the name, skip to be safe
                __result = null;
                return false;
            }

            // Allow original method to run
            return true;
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
            // Skip if result is null (prefix prevented save)
            if (__result == null)
                return;

            // Check if instance is still valid
            if (__instance == null || __instance.gameObject == null)
                return;

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
            // Skip prologue/loading scenes that might cause issues, but allow save loading
            if (!IsInMainScene() && !IsInLoadingScene())
                return true;

            if (saveData == null)
                return true;

            var baseData = saveData.ExtractBaseData<S1Datas.NPCData>();
            if (baseData == null || string.IsNullOrEmpty(baseData.ID))
                return true;

            var s1BaseNpc = FindBaseNpcById(baseData.ID);
            if (s1BaseNpc == null)
            {
                Logger.Warning($"[NPCPatches] NPCLoader_Load_Prefix: Could not find base NPC for '{baseData.ID}'.");
                Logger.Warning($"The above warning is normal when removing an S1API mod with custom NPCs, because the NPC is saved but doesn't exist anymore.");
                return true;
            }

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
                Logger.Warning(
                    $"NPCLoader_Load_Prefix: Exception in template check for ID '{baseData.ID}': {ex.Message}");
                return false;
            }

            var apiNpc = FindWrapperForS1Npc(s1BaseNpc);
            if (apiNpc == null || !apiNpc.IsCustomNPC)
            {
                return true; // run original for base NPCs
            }

            // Custom S1API NPC: perform safe subset of loading and skip original
            try
            {
                // Ensure dealer overflow slots are initialized BEFORE calling Load
                // This prevents null reference exceptions when Dealer.Load tries to load overflow items
                var dealerComponent = s1BaseNpc.GetComponent<S1Economy.Dealer>();
                if (dealerComponent != null)
                {
                    EnsureDealerOverflowSlots(dealerComponent);
                }

                s1BaseNpc.Load(saveData, baseData);

                // Check if relationship data exists in save
                if (saveData.TryGetData("Relationship", out S1Datas.RelationshipData rel) && rel != null && s1BaseNpc.RelationData != null)
                {
                    if (!float.IsNaN(rel.RelationDelta) && !float.IsInfinity(rel.RelationDelta))
                    {
                        s1BaseNpc.RelationData.SetRelationship(rel.RelationDelta);
                    }
                    
                    if (rel.Unlocked)
                    {
                        s1BaseNpc.RelationData.Unlock(rel.UnlockType, notify: false);
                        
                        // Store unlock type for potential restoration
                        try
                        {
                            apiNpc = FindWrapperForS1Npc(s1BaseNpc);
                            if (apiNpc != null)
                            {
                                var unlockTypeField = typeof(NPC).GetField("_loadedUnlockType", BindingFlags.NonPublic | BindingFlags.Instance);
                                if (unlockTypeField != null)
                                {
                                    var s1UnlockType = rel.UnlockType == S1Relation.NPCRelationData.EUnlockType.Recommendation
                                        ? S1Relation.NPCRelationData.EUnlockType.Recommendation
                                        : S1Relation.NPCRelationData.EUnlockType.DirectApproach;
                                    unlockTypeField.SetValue(apiNpc, s1UnlockType);
                                }
                            }
                        }
                        catch { }
                    }
                }
                
                // IMPORTANT: Mark as loaded from save IMMEDIATELY after processing relationship data
                // This must happen before FinalizeNetworkSpawn runs, otherwise defaults will overwrite loaded data
                try
                {
                    apiNpc = FindWrapperForS1Npc(s1BaseNpc);
                    if (apiNpc != null)
                    {
                        typeof(NPC).GetMethod("MarkLoadedFromSave", BindingFlags.NonPublic | BindingFlags.Instance)
                            ?.Invoke(apiNpc, null);
                    }
                }
                catch { }

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
                                        catch
                                        {
                                        }
                                    }
                                }
                            }

                            customerComponent.enabled = true;

                            ApplyCustomerDataSafely(customerComponent, cust);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning(
                                $"NPCLoader_Load_Prefix: Exception loading Customer data for '{baseData.ID}': {ex.Message}");
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
                        Logger.Warning(
                            $"NPCLoader_Load_Prefix: Exception loading Inventory data for '{baseData.ID}': {ex.Message}");
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
                    // Mark that this instance was hydrated from save data FIRST to prevent defaults overwrite
                    typeof(NPC).GetMethod("MarkLoadedFromSave", BindingFlags.NonPublic | BindingFlags.Instance)
                        ?.Invoke(wrap, null);
                    
                    var npcType = wrap.GetType();
                    bool hasDefaults = NPC.TypeToRelationshipDefaults.TryGetValue(npcType, out var relCfg) && relCfg != null;
                    
                    if (hasDefaults)
                    {
                        var builder = new NPCRelationshipDataBuilder();
                        relCfg(builder);
                        var rel = s1BaseNpc.RelationData;
                        if (rel != null)
                        {
                            // Preserve relationship delta if it was loaded from save (non-default value)
                            // Default relationship delta is 2.0, so if it's different, it came from save
                            float currentDelta = rel.RelationDelta;
                            bool deltaWasLoadedFromSave = Math.Abs(currentDelta - S1Relation.NPCRelationData.DEFAULT_RELATION_DELTA) > 0.01f;
                            
                            // Store the loaded delta before applying defaults
                            float savedDelta = currentDelta;
                            
                            bool beforeApplyDefaults = rel.Unlocked;
                            builder.ApplyTo(rel, s1BaseNpc, preserveUnlockState: true);
                            
                            // Restore relationship delta if it was loaded from save
                            if (deltaWasLoadedFromSave)
                            {
                                rel.SetRelationship(savedDelta);
                            }
                            
                            bool afterApplyDefaults = rel.Unlocked;
                            
                            if (beforeApplyDefaults && !afterApplyDefaults)
                            {
                                Logger.Warning($"[S1API] NPCLoader_Load_Prefix: WARNING - Unlock state was lost after applying defaults for '{baseData.ID}'!");
                            }
                        }
                        else
                        {
                            Logger.Warning($"[S1API] NPCLoader_Load_Prefix: RelationData is null when trying to apply defaults for '{baseData.ID}'");
                        }
                    }
                }
                else
                {
                    Logger.Warning($"[S1API] NPCLoader_Load_Prefix: Could not find wrapper NPC for '{baseData.ID}'");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] NPCLoader_Load_Prefix: Exception in post-load processing: {ex.Message}");
                Logger.Warning($"[S1API] Stack trace: {ex.StackTrace}");
            }

            return false; // skip original
        }

        /// <summary>
        /// Safely applies Customer data to S1API NPCs without triggering SyncVar setter issues.
        /// Uses reflection to access private/final/internal properties and fields to avoid network synchronization paths.
        /// </summary>
        private static void ApplyCustomerDataSafely(S1Economy.Customer customerComponent, S1Datas.CustomerData cust)
        {
            if (customerComponent == null || cust == null)
            {
                return;
            }

            try
            {
                var customerType = customerComponent.GetType();

                // Ensure internal data structures exist first
                try
                {
                    // Use reflection to access currentAffinityData field/property
                    PropertyInfo currentAffinityProp;
                    FieldInfo currentAffinityField;
                    currentAffinityField = customerType.GetField("currentAffinityData",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    currentAffinityProp = customerType.GetProperty("currentAffinityData",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                    S1Economy.CustomerAffinityData currentAffinity = null;
                    if (currentAffinityField != null)
                    {
                        if (currentAffinityField is FieldInfo field)
                        {
                            currentAffinity = field.GetValue(customerComponent) as S1Economy.CustomerAffinityData;
                        }
                        else if (currentAffinityProp is PropertyInfo prop)
                        {
                            currentAffinity = prop.GetValue(customerComponent) as S1Economy.CustomerAffinityData;
                        }
                    }

                    if (currentAffinity == null)
                    {
                        currentAffinity = new S1Economy.CustomerAffinityData();
                        var customerDataProp = customerType.GetProperty("CustomerData",
                            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                        if (customerDataProp != null)
                        {
                            var customerData = customerDataProp.GetValue(customerComponent);
                            if (customerData != null)
                            {
                                var defaultAffinityProp = customerData.GetType().GetProperty("DefaultAffinityData",
                                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                                var defaults = defaultAffinityProp?.GetValue(customerData);
                                if (defaults != null)
                                {
                                    var copyToMethod = defaults.GetType().GetMethod("CopyTo",
                                        BindingFlags.Public | BindingFlags.Instance);
                                    copyToMethod?.Invoke(defaults, new object[] { currentAffinity });
                                }
                            }
                        }

                        // Set the new currentAffinityData back
                        if (currentAffinityField is FieldInfo setField)
                        {
                            setField.SetValue(customerComponent, currentAffinity);
                        }
                        else if (currentAffinityProp is PropertyInfo setProp && setProp.CanWrite)
                        {
                            setProp.SetValue(customerComponent, currentAffinity);
                        }
                    }

                    if (cust.ProductAffinities != null && currentAffinity != null)
                    {
                        var productAffinitiesProp = currentAffinity.GetType().GetProperty("ProductAffinities",
                            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                        var productAffinities = productAffinitiesProp?.GetValue(currentAffinity);
                        if (productAffinities != null)
                        {
                            var countProperty = productAffinities.GetType()
                                .GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                            var count = countProperty != null ? (int)countProperty.GetValue(productAffinities) : 0;
                            var actualCount = Math.Min(cust.ProductAffinities.Length, count);

                            for (int i = 0; i < actualCount; i++)
                            {
                                if (!float.IsNaN(cust.ProductAffinities[i]))
                                {
                                    var indexer = productAffinities.GetType().GetProperty("Item",
                                        BindingFlags.Public | BindingFlags.Instance);
                                    var affinityItem =
                                        indexer?.GetMethod?.Invoke(productAffinities, new object[] { i });
                                    if (affinityItem != null)
                                    {
                                        var affinityProp = affinityItem.GetType().GetProperty("Affinity",
                                            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                                        affinityProp?.SetValue(affinityItem, cust.ProductAffinities[i]);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"ApplyCustomerDataSafely currentAffinityData: {ex.Message}");
                }

                try
                {
                    // Restore offered contract state lightly
                    if (cust.IsContractOffered && cust.OfferedContract != null)
                    {
                        var offeredContractInfoProp = customerType.GetProperty("OfferedContractInfo",
                            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                        var offeredContractTimeProp = customerType.GetProperty("OfferedContractTime",
                            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                        if (offeredContractInfoProp?.CanWrite == true)
                            offeredContractInfoProp.SetValue(customerComponent, cust.OfferedContract);
                        if (offeredContractTimeProp?.CanWrite == true)
                            offeredContractTimeProp.SetValue(customerComponent, cust.OfferedContractTime);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"ApplyCustomerDataSafely contract data: {ex.Message}");
                }

                try
                {
                    // Restore Dependence (CurrentAddiction) from save data
                    float currentAddiction = 0f;
                    var currentValue = Utils.ReflectionUtils.TryGetFieldOrProperty(customerComponent, "CurrentAddiction");
                    if (currentValue != null && currentValue is float)
                    {
                        currentAddiction = (float)currentValue;
                    }

                    float targetAddiction = cust.Dependence;
                    float delta = targetAddiction - currentAddiction;

                    // During load, ChangeAddiction doesn't work due to network sync issues
                    // Always use direct assignment during save loading
                    if (Math.Abs(delta) > 0.0001f)
                    {
                        Utils.ReflectionUtils.TrySetFieldOrProperty(customerComponent, "CurrentAddiction", targetAddiction);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"ApplyCustomerDataSafely dependence: {ex.Message}");
                }

                try
                {
                    // Basic counters - use reflection for inaccessible setters
                    var timeSinceLastDealCompletedProp = customerType.GetProperty("TimeSinceLastDealCompleted",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    var timeSinceLastDealOfferedProp = customerType.GetProperty("TimeSinceLastDealOffered",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    var offeredDealsProp = customerType.GetProperty("OfferedDeals",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    var completedDeliveriesProp = customerType.GetProperty("CompletedDeliveries",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    var timeSincePlayerApproachedProp = customerType.GetProperty("TimeSincePlayerApproached",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    var timeSinceInstantDealOfferedProp = customerType.GetProperty("TimeSinceInstantDealOffered",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                    if (timeSinceLastDealCompletedProp?.CanWrite == true)
                        timeSinceLastDealCompletedProp.SetValue(customerComponent, cust.TimeSinceLastDealCompleted);
                    if (timeSinceLastDealOfferedProp?.CanWrite == true)
                        timeSinceLastDealOfferedProp.SetValue(customerComponent, cust.TimeSinceLastDealOffered);
                    if (offeredDealsProp?.CanWrite == true)
                        offeredDealsProp.SetValue(customerComponent, cust.OfferedDeals);
                    if (completedDeliveriesProp?.CanWrite == true)
                        completedDeliveriesProp.SetValue(customerComponent, cust.CompletedDeals);
                    if (timeSincePlayerApproachedProp?.CanWrite == true)
                        timeSincePlayerApproachedProp.SetValue(customerComponent, cust.TimeSincePlayerApproached);
                    if (timeSinceInstantDealOfferedProp?.CanWrite == true)
                        timeSinceInstantDealOfferedProp.SetValue(customerComponent, cust.TimeSinceInstantDealOffered);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"ApplyCustomerDataSafely basic counters: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"ApplyCustomerDataSafely error: {ex.Message}");
            }
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
                    global::S1API.Map.Building.Unregister(building);
                }
            }
            catch
            {
            }

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
            catch
            {
            }

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
                        Logger.Warning(
                            $"NPCManager_GetSaveString_Prefix: Skipping NPC at index {i} due to exception: {ex.Message}");
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

                // Respect minimal fill - count non-empty slots (GetItemCount was removed in v0.4.2f4)
                int itemCount = 0;
                for (int j = 0; j < __instance.ItemSlots.Count; j++)
                {
                    if (__instance.ItemSlots[j]?.ItemInstance != null)
                        itemCount++;
                }
                if (itemCount >= 3)
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

                // Random items (local-only) - Use AddRandomItemsToInventory which handles the new API
                if (__instance.RandomItems && __instance.RandomInventoryItems != null &&
                    __instance.RandomInventoryItems.Length > 0)
                {
                    // v0.4.2f4 changed GetRandomInventoryItem to require excludeIDs parameter
                    // and added AddRandomItemsToInventory() as a public method - use that instead
                    __instance.AddRandomItemsToInventory();
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
            {
                Logger.Warning($"[NPCPatches] NPCLoader_Load_Postfix: No base NPC found for '{baseData.ID}'.");
                return;
            }

            var apiNpc = FindWrapperForS1Npc(s1BaseNpc);
            if (apiNpc == null)
                return;

            apiNpc.LoadFromDynamic(saveData);

            // Mark as loaded from save so prefab defaults won't overwrite
            try
            {
                typeof(NPC).GetMethod("MarkLoadedFromSave", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.Invoke(apiNpc, null);
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] NPCLoader_Load_Postfix: Exception marking NPC '{baseData.ID}' as loaded: {ex.Message}");
            }

            CustomNpcsReady = true;
        }

        /// <summary>
        /// Utility to find a base-game NPC by ID in a way compatible with both System and Il2Cpp lists.
        /// Also checks S1API NPC.All list as a fallback for custom NPCs that might not be in NPCRegistry yet.
        /// </summary>
        private static S1NPCs.NPC FindBaseNpcById(string id)
        {
            try
            {
                // First, check the base game registry
                var reg = S1NPCs.NPCManager.NPCRegistry;
                if (reg != null)
                {
                    for (int i = 0; i < reg.Count; i++)
                    {
                        var n = reg[i];
                        if (n != null && n.ID == id)
                            return n;
                    }
                }

                // Fallback: check S1API NPC.All list for custom NPCs that might not be registered yet
                // This can happen if NPCsLoader.Load runs before NPCs are fully registered
                for (int i = 0; i < NPC.All.Count; i++)
                {
                    var apiNpc = NPC.All[i];
                    if (apiNpc != null && apiNpc.S1NPC != null && apiNpc.S1NPC.ID == id)
                    {
                        // Ensure it's in the registry for future lookups
                        if (!S1NPCs.NPCManager.NPCRegistry.Contains(apiNpc.S1NPC))
                        {
                            S1NPCs.NPCManager.NPCRegistry.Add(apiNpc.S1NPC);
                        }
                        return apiNpc.S1NPC;
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
        /// Ensures dealer overflow slots are initialized before loading dealer data.
        /// Prevents null reference exceptions when Dealer.Load tries to load overflow items.
        /// </summary>
        private static void EnsureDealerOverflowSlots(S1Economy.Dealer dealer)
        {
            if (dealer == null)
                return;

            try
            {
                var overflowSlotsObj = Utils.ReflectionUtils.TryGetFieldOrProperty(dealer, "overflowSlots");
                
                // Check if slots exist and are properly initialized
                bool needsInit = false;
                int slotCount = 0;
                
                if (overflowSlotsObj == null)
                {
                    needsInit = true;
                }
                else
                {
                    // Try to get count/length regardless of array type
                    var countProp = overflowSlotsObj.GetType().GetProperty("Length") ?? 
                                   overflowSlotsObj.GetType().GetProperty("Count");
                    if (countProp != null)
                    {
                        slotCount = (int)countProp.GetValue(overflowSlotsObj);
                        needsInit = slotCount == 0;
                    }
                    else
                    {
                        needsInit = true;
                    }
                }

                var dealerId = dealer?.ID ?? dealer?.name ?? "<unknown>";

                if (needsInit)
                {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                    // Create IL2CPP array
                    var overflowSlots = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<S1Items.ItemSlot>(10);
                    for (int i = 0; i < 10; i++)
                    {
                        overflowSlots[i] = new S1Items.ItemSlot();
                        overflowSlots[i].SetSlotOwner(dealer.Cast<S1Items.IItemSlotOwner>());
                    }
#else
                    // Create regular C# array for Mono
                    var overflowSlots = new S1Items.ItemSlot[10];
                    for (int i = 0; i < 10; i++)
                    {
                        overflowSlots[i] = new S1Items.ItemSlot();
                        overflowSlots[i].SetSlotOwner(dealer);
                    }
#endif
                    
                    Utils.ReflectionUtils.TrySetFieldOrProperty(dealer, "overflowSlots", overflowSlots);
                }
                else if (slotCount > 0)
                {
                    // Slots exist, ensure they have proper owners
#if (IL2CPPMELON || IL2CPPBEPINEX)
                    // For IL2CPP, cast to Il2CppReferenceArray and use direct indexing
                    var il2cppArray = overflowSlotsObj as Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<S1Items.ItemSlot>;
                    if (il2cppArray != null)
                    {
                        for (int i = 0; i < slotCount && i < il2cppArray.Length; i++)
                        {
                            var slot = il2cppArray[i];
                            if (slot != null)
                            {
                                slot.SetSlotOwner(dealer.Cast<S1Items.IItemSlotOwner>());
                            }
                        }
                    }
#else
                    // For Mono, cast to regular array and use direct indexing
                    var monoArray = overflowSlotsObj as S1Items.ItemSlot[];
                    if (monoArray != null)
                    {
                        for (int i = 0; i < slotCount && i < monoArray.Length; i++)
                        {
                            var slot = monoArray[i];
                            if (slot != null)
                            {
                                slot.SetSlotOwner(dealer);
                            }
                        }
                    }
#endif
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception ensuring dealer overflow slots: {ex.Message}");
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

        /// <summary>
        /// Returns true when in a loading/persistence scene where save loading occurs.
        /// This allows S1API NPCs to be loaded properly during save restoration.
        /// </summary>
        private static bool IsInLoadingScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            // Allow loading in Loading, Persistence, or similar scenes
            return string.Equals(sceneName, "Loading", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(sceneName, "Persistence", StringComparison.OrdinalIgnoreCase) ||
                   sceneName.Contains("Load", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Fallback for NPCManager.GetNPC to resolve NPCs from S1API NPC.All list when the registry is missing entries.
        /// Ensures the registry is backfilled to keep future lookups fast and messaging lookups reliable.
        /// </summary>
        [HarmonyPatch(typeof(S1NPCs.NPCManager), nameof(S1NPCs.NPCManager.GetNPC))]
        [HarmonyPostfix]
        private static void NPCManager_GetNPC_Postfix(string id, ref S1NPCs.NPC __result)
        {
            if (__result != null)
                return;
            if (string.IsNullOrWhiteSpace(id))
                return;

            try
            {
                for (int i = 0; i < NPC.All.Count; i++)
                {
                    var apiNpc = NPC.All[i];
                    var baseNpc = apiNpc?.S1NPC;
                    if (baseNpc == null || string.IsNullOrEmpty(baseNpc.ID))
                        continue;

                    if (baseNpc.ID.Equals(id, StringComparison.OrdinalIgnoreCase))
                    {
                        __result = baseNpc;
                        try
                        {
                            var reg = S1NPCs.NPCManager.NPCRegistry;
                            if (reg != null && !reg.Contains(baseNpc))
                                reg.Add(baseNpc);
                        }
                        catch { }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"NPCManager_GetNPC_Postfix: exception while backfilling registry for id '{id}': {ex.Message}");
            }
        }

        /// <summary>
        /// Guard: Prevent SetGravityMultiplier from running if any ConstantForce components are null.
        /// This happens on instanced custom NPCs (prefabs created in menu work correctly) and
        /// logs NREs to Player.log
        /// </summary>
        /// <param name="__instance">NPCMovement instance</param>
        /// <param name="multiplier">Gravity multiplier value</param>
        [HarmonyPatch(typeof(S1NPCs.NPCMovement), nameof(S1NPCs.NPCMovement.SetGravityMultiplier))]
        [HarmonyPrefix]
        private static bool NPCMovement_SetGravityMultiplier_Prefix(S1NPCs.NPCMovement __instance, float multiplier)
        {
#if !IL2CPPMELON
            var ragdollForceComponentsField = typeof(S1NPCs.NPCMovement).GetField("ragdollForceComponents",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var ragdollForceComponents = ragdollForceComponentsField?.GetValue(__instance) as List<ConstantForce>;
#else
            var ragdollForceComponents = __instance.ragdollForceComponents;
#endif
            return ragdollForceComponents == null || ragdollForceComponents.ToArray().All(comp => comp != null);
        }

        /// <summary>
        /// Patch NPCHealth.Awake to defer TimeManager hookups via shim for custom S1API NPCs.
        /// This avoids NullReferenceExceptions during Awake when TimeManager is not yet initialized.
        /// </summary>
        [HarmonyPatch(typeof(S1NPCs.NPCHealth), "Awake")]
        [HarmonyPrefix]
        private static bool NPCHealth_Awake_Prefix(S1NPCs.NPCHealth __instance)
        {
            // fast path if TimeManager is initialized (for base NPCs)
            if (S1GameTime.TimeManager.InstanceExists) return true;

            // Recreate the Awake logic but defer TimeManager hookups via shim
            __instance.NetworkInitialize___Early();
            var npc = __instance.GetComponent<S1NPCs.NPC>();
#if (!IL2CPPMELON)
            var npcField = typeof(S1NPCs.NPCHealth)
                .GetField("npc", BindingFlags.NonPublic | BindingFlags.Instance);
            if (npcField != null)
                npcField.SetValue(__instance, npc);
            
            var sleepStartMethod = typeof(S1NPCs.NPCHealth)
                .GetMethod("SleepStart", BindingFlags.NonPublic | BindingFlags.Instance);
            var hourPassMethod = typeof(S1NPCs.NPCHealth)
                .GetMethod("OnHourPass", BindingFlags.NonPublic | BindingFlags.Instance);

            if (sleepStartMethod != null)
            {
                var sleepStartDelegate = 
                    (Action)Delegate.CreateDelegate(typeof(Action), __instance, sleepStartMethod);
                TimeManagerShim.Instance.onSleepStart =
                    (Action)Delegate.Remove(TimeManagerShim.Instance.onSleepStart, sleepStartDelegate)!;
                TimeManagerShim.Instance.onSleepStart =
                    (Action)Delegate.Combine(TimeManagerShim.Instance.onSleepStart, sleepStartDelegate);
            }

            if (hourPassMethod != null)
            {
                var hourPassDelegate = 
                    (Action)Delegate.CreateDelegate(typeof(Action), __instance, hourPassMethod);
                TimeManagerShim.Instance.onHourPass =
                    (Action)Delegate.Combine(TimeManagerShim.Instance.onHourPass, hourPassDelegate);
            }
#else
            __instance.npc = npc;

            TimeManagerShim.Instance.onSleepStart =
                (Action)Delegate.Combine(TimeManagerShim.Instance.onSleepStart, new Action(__instance.SleepStart));
            TimeManagerShim.Instance.onHourPass =
                (Action)Delegate.Combine(TimeManagerShim.Instance.onHourPass, new Action(__instance.OnHourPass));
#endif
            __instance.NetworkInitialize__Late();
            return false;
        }

        /// <summary>
        /// Preserves CurrentAddiction values loaded from save data by storing them before Customer.Awake runs.
        /// Customer.Awake resets CurrentAddiction to BaseAddiction, which would overwrite saved values during network spawn.
        /// </summary>
        [HarmonyPatch(typeof(S1Economy.Customer), "Awake")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Customer_Awake_Prefix(S1Economy.Customer __instance)
        {
            try
            {
                // Check if this customer has a saved CurrentAddiction value that we need to preserve
                var currentAddiction = Utils.ReflectionUtils.TryGetFieldOrProperty(__instance, "CurrentAddiction");
                if (currentAddiction != null && currentAddiction is float currentValue)
                {
                    // Store the value so we can restore it in Postfix
                    if (_savedCurrentAddiction.ContainsKey(__instance))
                        _savedCurrentAddiction[__instance] = currentValue;
                    else
                        _savedCurrentAddiction.Add(__instance, currentValue);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[NPCPatches] Customer_Awake_Prefix exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Restores CurrentAddiction values that were loaded from save data after Customer.Awake has run.
        /// This prevents the Awake method from overwriting saved values with BaseAddiction during network spawn.
        /// </summary>
        [HarmonyPatch(typeof(S1Economy.Customer), "Awake")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Customer_Awake_Postfix(S1Economy.Customer __instance)
        {
            try
            {
                // Check if we stored a saved CurrentAddiction value for this customer
                if (_savedCurrentAddiction.TryGetValue(__instance, out float savedValue))
                {
                    // Get the current value (which Awake just reset to BaseAddiction)
                    var currentAddiction = Utils.ReflectionUtils.TryGetFieldOrProperty(__instance, "CurrentAddiction");

                    // Only restore if the value actually changed (Awake reset it)
                    if (currentAddiction is float currentValue && Math.Abs(currentValue - savedValue) > 0.0001f)
                    {
                        Utils.ReflectionUtils.TrySetFieldOrProperty(__instance, "CurrentAddiction", savedValue);
                    }

                    // Clean up - remove from dictionary after processing
                    _savedCurrentAddiction.Remove(__instance);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[NPCPatches] Customer_Awake_Postfix exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Guard Dealer.Load to prevent ArgumentNullException when OverflowItems.LoadTo is called with null slots.
        /// Also delays customer assignment for ALL dealers to ensure custom NPCs are loaded before being assigned.
        /// Reimplements the dealer-specific parts of Load() safely, skipping the base NPC.Load call since
        /// the NPCLoader_Load_Prefix patch already handles that for custom NPCs.
        /// </summary>
        [HarmonyPatch(typeof(S1Economy.Dealer), nameof(S1Economy.Dealer.Load), new[] { typeof(S1Datas.DynamicSaveData), typeof(S1Datas.NPCData) })]
        [HarmonyPrefix]
        private static bool Dealer_Load_Prefix(S1Economy.Dealer __instance, S1Datas.DynamicSaveData dynamicData, S1Datas.NPCData npcData)
        {
            var baseNpc = __instance as S1NPCs.NPC;
            if (baseNpc == null)
                return true; // Run original for non-NPC Dealers (shouldn't happen)

            // Prevent infinite recursion - if we're already loading this dealer, skip the patch
            string dealerId = baseNpc.ID;
            if (_loadingDealers.Contains(dealerId))
                return true; // Already processing this dealer, let original run

            var apiNpc = FindWrapperForS1Npc(baseNpc);
            bool isCustomNPC = apiNpc != null && apiNpc.IsCustomNPC;
            
            // Extract dealer data to check for custom NPC customers
            if (!dynamicData.TryExtractBaseData<S1Datas.DealerData>(out var data))
            {
                Logger.Warning($"[NPCPatches] Dealer_Load_Prefix: '{dealerId}' missing DealerData. Falling back to original.");
                return true; // Let original handle if we can't extract data
            }
            
            // Check if any assigned customers are custom NPCs or not yet loaded
            bool needsDelayedAssignment = false;
            if (data.AssignedCustomerIDs != null && data.AssignedCustomerIDs.Length > 0)
            {
                foreach (string customerID in data.AssignedCustomerIDs)
                {
                    if (string.IsNullOrEmpty(customerID))
                        continue;
                    
                    // Check if this customer ID is a custom NPC
                    bool isCustomCustomer = NPC.All.Any(n => n.S1NPC?.ID == customerID);
                    
                    // Check if customer exists in registry yet
                    bool customerExists = false;
                    try
                    {
                        var reg = S1NPCs.NPCManager.NPCRegistry;
                        if (reg != null)
                        {
                            for (int i = 0; i < reg.Count; i++)
                            {
                                if (reg[i]?.ID == customerID)
                                {
                                    customerExists = true;
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                    
                    // If it's a custom NPC or doesn't exist yet, we need delayed assignment
                    if (isCustomCustomer || !customerExists)
                    {
                        needsDelayedAssignment = true;
                        break;
                    }
                }
            }
            
            // Only intercept for custom NPCs OR if we need delayed customer assignment
            if (!isCustomNPC && !needsDelayedAssignment)
                return true; // Run original for base NPCs with only base NPC customers

            // Mark as loading to prevent recursion
            _loadingDealers.Add(dealerId);

            try
            {
                // For base game dealers, we need to call NPC.Load first (NPCLoader_Load_Prefix handles custom NPCs)
                // This will internally call Dealer.Load, but our recursion guard will prevent infinite loop
                if (!isCustomNPC)
                {
                    baseNpc.Load(dynamicData, npcData);
                }
                // Note: For custom NPCs, NPCLoader_Load_Prefix already called NPC.Load for us
                
                // Extract dealer data (we already extracted it above, but need it again for the rest)
                if (!dynamicData.TryExtractBaseData<S1Datas.DealerData>(out var dealerData))
                {
                    return false; // Skip original
                }
                
                if (dealerData.Recruited)
                    __instance.SetIsRecruited(null);
                
                __instance.SetCash(dealerData.Cash);

                // Assign customers - use a coroutine to ensure all NPCs are loaded first
                if (dealerData.AssignedCustomerIDs != null && dealerData.AssignedCustomerIDs.Length > 0)
                {
                    MelonCoroutines.Start(DelayedCustomerAssignment(__instance, dealerData.AssignedCustomerIDs));
                }

                // Restore contracts
                if (dealerData.ActiveContractGUIDs != null)
                {
                    for (int j = 0; j < dealerData.ActiveContractGUIDs.Length; j++)
                    {
#if MONOMELON
                        if (!GUIDManager.IsGUIDValid(dealerData.ActiveContractGUIDs[j]))
#else
                        if (!Il2Cpp.GUIDManager.IsGUIDValid(dealerData.ActiveContractGUIDs[j]))
#endif
                        {
                            Logger.Warning($"Invalid contract GUID: {dealerData.ActiveContractGUIDs[j]}");
                            continue;
                        }
#if MONOMELON
                        var contract = GUIDManager.GetObject<S1Quests.Contract>(new Guid(dealerData.ActiveContractGUIDs[j]));
#else
                        var contract = Il2Cpp.GUIDManager.GetObject<S1Quests.Contract>(new Il2CppSystem.Guid(dealerData.ActiveContractGUIDs[j]));
#endif
                        
                        if (contract != null)
                        {
                            Type dealerType = typeof(S1Economy.Dealer);
                            MethodInfo? addContractMethod = dealerType.GetMethod("AddContract",  BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (addContractMethod != null)
                                addContractMethod.Invoke(__instance, [contract]);
                        }
                    }
                }

                if (dealerData.HasBeenRecommended)
                    __instance.MarkAsRecommended();

                // overflow items loading - ensure slots exist first
                if (dealerData.OverflowItems != null)
                {
                    try
                    {
                        EnsureDealerOverflowSlots(__instance);
                        
                        var overflowSlotsObj = Utils.ReflectionUtils.TryGetFieldOrProperty(__instance, "overflowSlots");
                        
                        if (overflowSlotsObj != null)
                        {
                            // Get the array length/count
                            var countProp = overflowSlotsObj.GetType().GetProperty("Length") ?? 
                                           overflowSlotsObj.GetType().GetProperty("Count");
                            int slotCount = countProp != null ? (int)countProp.GetValue(overflowSlotsObj) : 0;
                            
                            if (slotCount > 0)
                            {
                                // Convert to ItemSlot array for LoadTo
                                var slotsArray = new S1Items.ItemSlot[slotCount];
                                bool hasNullSlots = false;
                                
#if (IL2CPPMELON || IL2CPPBEPINEX)
                                // For IL2CPP, cast to Il2CppReferenceArray and use direct indexing
                                var il2cppArray = overflowSlotsObj as Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<S1Items.ItemSlot>;
                                if (il2cppArray != null)
                                {
                                    for (int i = 0; i < slotCount && i < il2cppArray.Length; i++)
                                    {
                                        var slot = il2cppArray[i];
                                        if (slot == null)
                                        {
                                            hasNullSlots = true;
                                            // Create a new slot if null
                                            slot = new S1Items.ItemSlot();
                                            slot.SetSlotOwner(__instance.Cast<S1Items.IItemSlotOwner>());
                                            // Note: Can't directly set array elements in IL2CPP arrays via reflection
                                            // The slot will be created but may not persist in the array
                                        }
                                        slotsArray[i] = slot;
                                    }
                                }
#else
                                // For Mono, cast to regular array and use direct indexing
                                var monoArray = overflowSlotsObj as S1Items.ItemSlot[];
                                if (monoArray != null)
                                {
                                    for (int i = 0; i < slotCount && i < monoArray.Length; i++)
                                    {
                                        var slot = monoArray[i];
                                        if (slot == null)
                                        {
                                            hasNullSlots = true;
                                            // Create a new slot if null
                                            slot = new S1Items.ItemSlot();
                                            slot.SetSlotOwner(__instance);
                                            monoArray[i] = slot; // Can set directly in Mono arrays
                                        }
                                        slotsArray[i] = slot;
                                    }
                                }
#endif

                                if (dealerData.OverflowItems != null && slotsArray != null && slotsArray.Length > 0)
                                {
                                    dealerData.OverflowItems.LoadTo(slotsArray);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to load overflow items for dealer '{baseNpc?.ID ?? "unknown"}': {ex.Message}");
                        Logger.Warning($"Stack trace: {ex.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Dealer_Load_Prefix failed for '{baseNpc?.ID}': {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                // Always remove from loading set, even on error
                _loadingDealers.Remove(dealerId);
            }

            return false; // Skip original for custom NPCs
        }

        /// <summary>
        /// Guards GetActionsTotallyOccurringWithinRange to skip pre-created disabled NPCAction components
        /// from S1API's prefab pooling system. These components are inactive and should not be processed
        /// until they are properly configured and activated by the schedule runtime.
        /// </summary>
        [HarmonyPatch(typeof(S1NPCs.NPCScheduleManager), "GetActionsTotallyOccurringWithinRange")]
        [HarmonyPrefix]
        private static bool NPCScheduleManager_GetActionsTotallyOccurringWithinRange_Prefix(
            S1NPCs.NPCScheduleManager __instance,
            int min,
            int max,
            bool checkShouldStart,
#if (IL2CPPMELON || IL2CPPBEPINEX)
            ref Il2CppSystem.Collections.Generic.List<S1NPCsSchedules.NPCAction> __result)
#else
            ref System.Collections.Generic.List<S1NPCsSchedules.NPCAction> __result)
#endif
        {
            if (__instance == null)
                return true; // Run original if instance is null

            try
            {
                // Get the ActionList from the schedule manager
                var actionListObj = Utils.ReflectionUtils.TryGetFieldOrProperty(__instance, "ActionList");
                if (actionListObj == null)
                    return true; // Run original if we can't get the list

                // Handle both System.List and Il2CppList types
                int actionCount = 0;
#if (IL2CPPMELON || IL2CPPBEPINEX)
                var il2cppList = actionListObj as Il2CppSystem.Collections.Generic.List<S1NPCsSchedules.NPCAction>;
                if (il2cppList != null)
                    actionCount = il2cppList.Count;
                else
                    return true; // Unexpected type, fall back to original
#else
                var monoList = actionListObj as List<S1NPCsSchedules.NPCAction>;
                if (monoList != null)
                    actionCount = monoList.Count;
                else
                    return true; // Unexpected type, fall back to original
#endif

#if (IL2CPPMELON || IL2CPPBEPINEX)
                var list = new Il2CppSystem.Collections.Generic.List<S1NPCsSchedules.NPCAction>();
#else
                var list = new List<S1NPCsSchedules.NPCAction>();
#endif

                // Iterate through actions, skipping null or inactive (pre-created) ones
                for (int i = 0; i < actionCount; i++)
                {
                    S1NPCsSchedules.NPCAction action = null;
#if (IL2CPPMELON || IL2CPPBEPINEX)
                    action = il2cppList[i];
#else
                    action = monoList[i];
#endif

                    // Skip null actions
                    if (action == null)
                        continue;

                    // Skip inactive/disabled actions (pre-created pool components)
                    if (action.gameObject == null || !action.gameObject.activeInHierarchy)
                        continue;

                    // Check if action should start and if it occurs within the time range
                    if ((!checkShouldStart || action.ShouldStart()) &&
                        S1GameTime.TimeManager.IsGivenTimeWithinRange(action.StartTime, min, max) &&
                        S1GameTime.TimeManager.IsGivenTimeWithinRange(action.GetEndTime(), min, max))
                    {
                        list.Add(action);
                    }
                }

                // Sort by priority (descending)
                try
                {
                    var orderByDescending = Utils.ReflectionUtils.TryGetFieldOrProperty(__instance, "orderByDescending");
                    if (orderByDescending != null)
                    {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                        var comparer = orderByDescending as Il2CppSystem.Collections.Generic.IComparer<S1NPCsSchedules.NPCAction>;
                        if (comparer != null)
                            list.Sort(comparer);
#else
                        var comparer = orderByDescending as System.Collections.Generic.IComparer<S1NPCsSchedules.NPCAction>;
                        if (comparer != null)
                            list.Sort(comparer);
#endif
                    }
                }
                catch
                {
                    // If sorting fails, continue without sorting
                }

                __result = list;
                return false; // Skip original method
            }
            catch (Exception ex)
            {
                Logger.Warning($"NPCScheduleManager_GetActionsTotallyOccurringWithinRange_Prefix failed: {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
                return true; // Fall back to original on error
            }
        }

        /// <summary>
        /// Fixes the inconsistent comparison function in NPCScheduleManager.InitializeActions()
        /// that causes sort failures when multiple actions have the same StartTime.
        /// The original comparison only checks a.IsSignal without comparing to b.IsSignal,
        /// violating the comparison contract (Compare(a,b) must equal -Compare(b,a)).
        /// </summary>
        /// <param name="__instance">NPCScheduleManager instance</param>
        [HarmonyPatch(typeof(S1NPCs.NPCScheduleManager), nameof(S1NPCs.NPCScheduleManager.InitializeActions))]
        [HarmonyPrefix]
        private static bool NPCScheduleManager_InitializeActions_Prefix(S1NPCs.NPCScheduleManager __instance)
        {
            if (__instance == null)
                return true; // Run original if instance is null

            try
            {
                // Get all actions including inactive ones (for S1API pooling)
                var actionsArray = __instance.gameObject.GetComponentsInChildren<S1NPCsSchedules.NPCAction>(includeInactive: true);
                
                // Create appropriate list type for the platform
#if (IL2CPPMELON || IL2CPPBEPINEX)
                var list = new Il2CppSystem.Collections.Generic.List<S1NPCsSchedules.NPCAction>();
#else
                var list = new List<S1NPCsSchedules.NPCAction>();
#endif
                
                // Add all actions to the list
                for (int i = 0; i < actionsArray.Length; i++)
                {
                    if (actionsArray[i] != null)
                        list.Add(actionsArray[i]);
                }
                
                // Sort with fixed comparison function
                // Use manual bubble sort for IL2CPP compatibility (can't use delegates or IComparer easily)
#if (IL2CPPMELON || IL2CPPBEPINEX)
                // Manual sort for IL2CPP - convert to array, sort, rebuild list
                var sortArray = new S1NPCsSchedules.NPCAction[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    sortArray[i] = list[i];
                }
                
                // Simple bubble sort (acceptable for small action lists)
                for (int i = 0; i < sortArray.Length - 1; i++)
                {
                    for (int j = 0; j < sortArray.Length - i - 1; j++)
                    {
                        if (CompareNPCActions(sortArray[j], sortArray[j + 1]) > 0)
                        {
                            var temp = sortArray[j];
                            sortArray[j] = sortArray[j + 1];
                            sortArray[j + 1] = temp;
                        }
                    }
                }
                
                // Rebuild the list
                list.Clear();
                for (int i = 0; i < sortArray.Length; i++)
                {
                    list.Add(sortArray[i]);
                }
#else
                // Mono can use Comparison<T> delegate
                list.Sort(CompareNPCActions);
#endif

                // Editor-only: rename objects with time descriptions
                if (!UnityEngine.Application.isPlaying)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        if (item != null && item.transform != null)
                        {
                            item.transform.name = item.GetName() + " (" + item.GetTimeDescription() + ")";
                            item.transform.SetAsLastSibling();
                        }
                    }
                }

                // Set the sorted list (use ReflectionUtils to handle both Mono fields and IL2CPP properties)
                bool setSuccess = Utils.ReflectionUtils.TrySetFieldOrProperty(__instance, "ActionList", list);
                if (!setSuccess)
                {
                    Logger.Warning($"NPCScheduleManager_InitializeActions_Prefix: Failed to set ActionList on {__instance?.GetType().Name ?? "null"}");
                    // Try to get the property/field type to debug
                    try
                    {
                        var prop = __instance.GetType().GetProperty("ActionList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        var field = __instance.GetType().GetField("ActionList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        var memberType = prop?.PropertyType ?? field?.FieldType;
                        Logger.Warning($"ActionList member type: {memberType?.FullName ?? "null"}, list type: {list.GetType().FullName}");
                    }
                    catch { }
                    return true; // Fall back to original on failure
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"NPCScheduleManager_InitializeActions_Prefix failed: {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
                return true; // Fall back to original on error
            }

            return false; // Skip original method
        }

        /// <summary>
        /// Coroutine to assign customers to a dealer after a delay to ensure all NPCs are loaded.
        /// </summary>
        private static IEnumerator DelayedCustomerAssignment(S1Economy.Dealer dealer, string[] customerIDs)
        {
            // Wait a few frames to ensure all NPCs are loaded
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);

            if (dealer == null || customerIDs == null)
                yield break;

            string dealerId = "<unknown>";
            try
            {
                dealerId = dealer.ID ?? dealer.name ?? "<unknown>";
            }
            catch { }

            // Process each customer assignment
            for (int i = 0; i < customerIDs.Length; i++)
            {
                var customerID = customerIDs[i];
                if (string.IsNullOrEmpty(customerID))
                    continue;

                // Try to find the customer NPC - retry a few times if not found
                S1NPCs.NPC npc = null;
                for (int retry = 0; retry < 5 && npc == null; retry++)
                {
                    try
                    {
                        npc = S1NPCs.NPCManager.GetNPC(customerID);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Exception finding customer NPC {customerID} (retry {retry}): {ex.Message}");
                    }

                    if (npc == null && retry < 4)
                    {
                        yield return new WaitForSeconds(0.1f);
                    }
                }

                if (npc == null)
                {
                    Logger.Warning($"Failed to find customer NPC with ID {customerID} after retries");
                    continue;
                }

                S1Economy.Customer customer = null;
                try
                {
                    customer = npc.GetComponent<S1Economy.Customer>();
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Exception getting Customer component for {customerID}: {ex.Message}");
                }

                if (customer == null)
                {
                    Logger.Warning($"NPC {npc.fullName} (ID: {customerID}) is not a customer");
                    continue;
                }

                try
                {
                    dealer.AddCustomer_Server(customer.NPC.ID);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Exception assigning customer {customerID} to dealer: {ex.Message}");
                }
            }
        }
    }
}
