#if (IL2CPPMELON)
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Interaction = Il2CppScheduleOne.Interaction;
using S1Messaging = Il2CppScheduleOne.Messaging;
using S1Noise = Il2CppScheduleOne.Noise;
using S1Economy = Il2CppScheduleOne.Economy;
using S1Relation = Il2CppScheduleOne.NPCs.Relation;
using S1Responses = Il2CppScheduleOne.NPCs.Responses;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
using S1ContactApps = Il2CppScheduleOne.UI.Phone.ContactsApp;
using S1WorkspacePopup = Il2CppScheduleOne.UI.WorldspacePopup;
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1Behaviour = Il2CppScheduleOne.NPCs.Behaviour;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
using S1Vision = Il2CppScheduleOne.Vision;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1Combat = Il2CppScheduleOne.Combat;
using S1Items = Il2CppScheduleOne.ItemFramework;
using S1MapBase = Il2CppScheduleOne.Map;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1Registry = Il2CppScheduleOne.Registry;
using S1Money = Il2CppScheduleOne.Money;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Interaction = ScheduleOne.Interaction;
using S1Messaging = ScheduleOne.Messaging;
using S1Noise = ScheduleOne.Noise;
using S1Economy = ScheduleOne.Economy;
using S1Relation = ScheduleOne.NPCs.Relation;
using S1Responses = ScheduleOne.NPCs.Responses;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
using S1ContactApps = ScheduleOne.UI.Phone.ContactsApp;
using S1WorkspacePopup = ScheduleOne.UI.WorldspacePopup;
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1Behaviour = ScheduleOne.NPCs.Behaviour;
using S1Vehicles = ScheduleOne.Vehicles;
using S1Vision = ScheduleOne.Vision;
using S1NPCs = ScheduleOne.NPCs;
using S1Combat = ScheduleOne.Combat;
using S1Items = ScheduleOne.ItemFramework;
using S1MapBase = ScheduleOne.Map;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1Registry = ScheduleOne.Registry;
using S1Money = ScheduleOne.Money;
#endif

#if (IL2CPPBEPINEX || IL2CPPMELON)
using S1Type = Il2CppSystem.Type;
using Il2CppInterop.Runtime;
#else
using S1Type = System.Type;
#endif

#if (IL2CPPBEPINEX || IL2CPPMELON)
using Il2CppSystem.Collections.Generic;
#else
using System.Collections.Generic;
#endif

#if (IL2CPPMELON)
using ConversationCategoryList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Messaging.EConversationCategory>;
#else
using ConversationCategoryList = System.Collections.Generic.List<ScheduleOne.Messaging.EConversationCategory>;
#endif

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
#if (IL2CPPMELON)
using Il2CppFishNet;
using Il2CppFishNet.Managing;
using Il2CppFishNet.Managing.Object;
using Il2CppFishNet.Object;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Object;
#endif
using MelonLoader;
using S1API.Entities.Behaviour;
using S1API.Entities.Interfaces;
using S1API.Entities.Schedule;
using S1API.Entities.Customer;
using S1API.Entities.Equippables;
using S1API.Entities.Dealer;
using S1API.Entities.Relation;
using S1API.Internal;
using S1API.Internal.Abstraction;
using S1API.Internal.Entities;
using S1API.Map;
using S1API.Messaging;
using S1API.Logging;
using S1API.Vehicles;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace S1API.Entities
{
    /// <summary>
    /// Abstract base class for creating custom NPCs with modular architecture supporting both physical and non-physical NPCs.
    /// Physical NPCs are visible in the game world with 3D models, movement, and direct interaction.
    /// Non-physical NPCs are invisible contacts primarily used for messaging and phone interactions.
    /// </summary>
    /// <remarks>
    /// NPCs provide access to component systems: <see cref="Appearance"/>, <see cref="Dialogue"/>, <see cref="Schedule"/>,
    /// <see cref="Customer"/>, <see cref="Relationship"/>, <see cref="Inventory"/>, and <see cref="Movement"/>.
    /// Customer, relationship, and schedule configuration must be done in <see cref="ConfigurePrefab"/> for proper save/load behavior.
    /// </remarks>
    public abstract class NPC : Saveable, IEntity, IHealth
    {
        private static readonly Log Logger = new Log("NPC");
        // Protected members intended to be used by modders.
        // Intended to be used from within the class / derived classes ONLY.
        private static readonly System.Collections.Generic.Dictionary<System.Type, GameObject> TypeToPrefab = new System.Collections.Generic.Dictionary<System.Type, GameObject>();
        private static readonly object TemplateLoadLock = new object();
        private static readonly System.Collections.Generic.Dictionary<System.Type, System.Collections.Generic.List<IScheduleActionSpec>> TypeToSchedulePlan = new System.Collections.Generic.Dictionary<System.Type, System.Collections.Generic.List<IScheduleActionSpec>>();
        private static readonly System.Collections.Generic.Dictionary<System.Type, System.Action<CustomerDataBuilder>> TypeToCustomerDefaults = new System.Collections.Generic.Dictionary<System.Type, System.Action<CustomerDataBuilder>>();
        internal static readonly System.Collections.Generic.Dictionary<System.Type, System.Action<NPCRelationshipDataBuilder>> TypeToRelationshipDefaults = new System.Collections.Generic.Dictionary<System.Type, System.Action<NPCRelationshipDataBuilder>>();
        private static readonly System.Collections.Generic.Dictionary<System.Type, System.Action<RandomInventoryItemsBuilder>> TypeToRandomInventoryDefaults = new System.Collections.Generic.Dictionary<System.Type, System.Action<RandomInventoryItemsBuilder>>();
        private static readonly System.Collections.Generic.Dictionary<System.Type, System.Action<DealerDataBuilder>> TypeToDealerDefaults = new System.Collections.Generic.Dictionary<System.Type, System.Action<DealerDataBuilder>>();
        private static readonly System.Collections.Generic.Dictionary<System.Type, (Vector3 position, Quaternion rotation)> TypeToSpawnPosition = new System.Collections.Generic.Dictionary<System.Type, (Vector3, Quaternion)>();
        private static readonly System.Collections.Generic.HashSet<System.Type> CustomerTypes = new System.Collections.Generic.HashSet<System.Type>();
        private static readonly System.Collections.Generic.HashSet<System.Type> DealerTypes = new System.Collections.Generic.HashSet<System.Type>();
        private static volatile bool _prefabsConfiguredForLocalProcess;
        internal static bool PrefabsConfiguredForLocalProcess => _prefabsConfiguredForLocalProcess;
        private S1AvatarFramework.Avatar? _runtimeAvatar;
        
        #region Template Prefab Helpers

        private static GameObject InstantiateTemplateInstance(System.Type npcType, NPC owner)
        {
            GameObject prefab = GetOrCreatePerNpcPrefab(npcType, owner);
            NetworkObject netPrefab = prefab.GetComponent<NetworkObject>() ?? prefab.AddComponent<NetworkObject>();

            NetworkObject spawnableNetPrefab = null;
            try
            {
                var nm = InstanceFinder.NetworkManager;
                if (nm != null)
                {
                    PrefabObjects spawnablePrefabs = nm.SpawnablePrefabs;
                    if (spawnablePrefabs != null)
                    {
                        int count = spawnablePrefabs.GetObjectCount();
                        for (int i = 0; i < count; i++)
                        {
                            NetworkObject obj = spawnablePrefabs.GetObject(true, i);
                            if (obj != null && obj.gameObject != null && obj.gameObject.name == prefab.name)
                            {
                                spawnableNetPrefab = obj;
                                break;
                            }
                        }
                    }
                }
            }
            catch { /* no-op: fallback to local prefab */ }

            GameObject prefabToUse = spawnableNetPrefab?.gameObject ?? prefab;
            GameObject instance = UnityEngine.Object.Instantiate<GameObject>(prefabToUse);
            try
            {
                var nm = InstanceFinder.NetworkManager;
                bool isServer = nm != null && nm.IsServer;
                var existingNo = instance.GetComponent<NetworkObject>();
                if (isServer)
                {
                    if (existingNo == null)
                        existingNo = instance.AddComponent<NetworkObject>();
                }
                else
                {
                    if (existingNo != null)
                        UnityEngine.Object.Destroy(existingNo);
                }
            }
            catch { }
            if (S1NPCs.NPCManager.InstanceExists && S1NPCs.NPCManager.Instance.NPCContainer != null)
            {
                Transform parent = S1NPCs.NPCManager.Instance.NPCContainer;
                if (parent != null && parent.gameObject != null && parent.gameObject.activeInHierarchy)
                    instance.transform.SetParent(parent, false);
            }
            instance.name = prefab.name;
            return instance;
        }

        private static GameObject GetOrCreatePerNpcPrefab(System.Type npcType, NPC owner)
        {
            if (npcType == null)
                throw new Exception("NPC type is null for prefab resolution.");

            if (TypeToPrefab.TryGetValue(npcType, out var cached) && cached != null)
            {
                MarkPrefabsConfigured();
                return cached;
            }

            lock (TemplateLoadLock)
            {
                if (TypeToPrefab.TryGetValue(npcType, out cached) && cached != null)
                {
                    MarkPrefabsConfigured();
                    return cached;
                }

                // Prefer a spawnable prefab provided by the base game.
                // For dealer types, use "Dealer" prefab; otherwise use "BaseNPC".
                var nm = InstanceFinder.NetworkManager;
                if (nm == null)
                    throw new Exception("NetworkManager not found when resolving NPC prefab.");

                PrefabObjects spawnablePrefabs = nm.SpawnablePrefabs;
                if (spawnablePrefabs == null)
                    throw new Exception("SpawnablePrefabs not available on NetworkManager.");

                NetworkObject chosen = null;
                int count = spawnablePrefabs.GetObjectCount();
                
                // Check if this NPC type is a dealer type by checking the IsDealer property
                bool isDealerType = false;
                try
                {
                    // Create a temporary instance to check IsDealer property
                    NPC tempInstance = (NPC)FormatterServices.GetUninitializedObject(npcType);
                    isDealerType = tempInstance.IsDealer;
                }
                catch
                {
                    // Fallback to checking if already registered as dealer type
                    isDealerType = IsDealerType(npcType);
                }
                
                if (isDealerType)
                {
                    // First, try to find a prefab named "Dealer"
                    for (int i = 0; i < count; i++)
                    {
                        NetworkObject obj = spawnablePrefabs.GetObject(true, i);
                        if (obj != null && obj.gameObject != null && obj.gameObject.name == "Dealer")
                        {
                            chosen = obj;
                            break;
                        }
                    }
                    
                    // If "Dealer" was not found, look for any spawnable containing a Dealer component
                    if (chosen == null)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            NetworkObject obj = spawnablePrefabs.GetObject(true, i);
                            if (obj != null && obj.gameObject != null && obj.gameObject.GetComponent<S1Economy.Dealer>() != null)
                            {
                                chosen = obj;
                                break;
                            }
                        }
                    }
                }
                
                // If not a dealer type or dealer prefab not found, use BaseNPC logic
                if (chosen == null)
                {
                    // Look for "BaseNPC" prefab
                    for (int i = 0; i < count; i++)
                    {
                        NetworkObject obj = spawnablePrefabs.GetObject(true, i);
                        if (obj != null && obj.gameObject != null && obj.gameObject.name == "BaseNPC")
                        {
                            chosen = obj;
                            break;
                        }
                    }
                }

                // If "BaseNPC" was not found, look for any spawnable containing the base NPC component.
                if (chosen == null)
                {
                    for (int i = 0; i < count; i++)
                    {
                        NetworkObject obj = spawnablePrefabs.GetObject(true, i);
                        if (obj != null && obj.gameObject != null && obj.gameObject.GetComponent<S1NPCs.NPC>() != null)
                        {
                            chosen = obj;
                            break;
                        }
                    }
                }

                if (chosen == null)
                {
                    string expectedPrefabName = isDealerType ? "Dealer" : "BaseNPC";
                    string expectedComponent = isDealerType ? "Dealer" : "NPC";
                    throw new Exception($"Failed to locate a suitable NPC spawnable prefab ({expectedPrefabName} or any with {expectedComponent} component).");
                }

                // Build a unique per-NPC prefab based on type
                NetworkObject prefabNO = UnityEngine.Object.Instantiate<NetworkObject>(chosen);
                string prefabName = GetPrefabNameForType(npcType);
                prefabNO.gameObject.name = prefabName;

                // Ensure template prefab does not execute runtime logic or remain in NPC registry
                try
                {
                    // Deactivate template instance to prevent Awake/Start side effects
                    if (prefabNO != null && prefabNO.gameObject != null)
                    {
                        prefabNO.gameObject.SetActive(false);
                        
                        // Handle registry cleanup for both NPC and Dealer components
                        var dealerComp = prefabNO.gameObject.GetComponent<S1Economy.Dealer>();
                        var npcComp = dealerComp != null ? dealerComp as S1NPCs.NPC : prefabNO.gameObject.GetComponent<S1NPCs.NPC>();
                        
                        if (npcComp != null)
                        {
                            var reg = S1NPCs.NPCManager.NPCRegistry;
                            if (reg != null && reg.Count > 0)
                            {
                                for (int i = reg.Count - 1; i >= 0; i--)
                                {
                                    if (reg[i] == npcComp)
                                    {
                                        reg.RemoveAt(i);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                // Let the NPC subclass declare required components on the prefab (Customer, actions, etc.)
                var builder = new NPCPrefabBuilder(prefabNO.gameObject, npcType);
                if (owner != null)
                {
                    owner.ConfigurePrefab(builder);
                }
                else
                {
                    InvokeConfigurePrefabWithoutInstance(npcType, builder);
                }

                // Ensure schedule actions exist on the template so NetworkBehaviour indices are stable
                try
                {
                    EnsureScheduleActionsOnPrefab(prefabNO.gameObject);
                }
                catch { }

                // If we are pre-registering without an instance owner, ensure baseline Customer exists when applicable
                if (owner == null)
                {
                    try
                    {
                        // Only add Customer for types that opted-in via EnsureCustomer
                        if (IsCustomerType(npcType))
                        {
                            var existingCustomer = prefabNO.gameObject.GetComponent<S1Economy.Customer>();
                            if (existingCustomer == null)
                            {
                                existingCustomer = prefabNO.gameObject.AddComponent<S1Economy.Customer>();
                            }

                            // Apply defaults if the mod registered them
                            var defaults = GetCustomerDefaultsForType(npcType);
                            if (defaults != null && existingCustomer != null)
                            {
                                var data = BuildCustomerDefaultsForType(npcType);
                                TrySetCustomerDataOnComponent(existingCustomer, data);
                            }
                        }
                        
                        // Handle dealer conversion: if dealer type, check if we already have a Dealer component
                        if (IsDealerType(npcType))
                        {
                            var existingDealer = prefabNO.gameObject.GetComponent<S1Economy.Dealer>();
                            
                            // If Dealer already exists (from using Dealer prefab), apply dealer defaults
                            if (existingDealer != null)
                            {
                                var dealerDefaults = BuildDealerDefaultsForType(npcType);
                                if (dealerDefaults != null)
                                {
                                    TryApplyDealerDefaults(existingDealer, dealerDefaults);
                                }
                            }
                            else
                            {
                                // If we're using BaseNPC prefab but need dealer functionality, warn
                                Logger.Warning($"[S1API] NPC {npcType.Name} requested dealer functionality but prefab does not have Dealer component. EnsureDealer() was called before prefab creation.");
                            }
                        }
                    }
                    catch { }
                }

                // Register as spawnable so FishNet assigns stable behaviour indices and can network-spawn
                try
                {
                    if (spawnablePrefabs != null)
                    {
                        bool alreadyRegistered = false;
                        int existingCount = spawnablePrefabs.GetObjectCount();
                        for (int i = 0; i < existingCount; i++)
                        {
                            NetworkObject existing = spawnablePrefabs.GetObject(true, i);
                            if (existing != null && existing.gameObject != null && existing.gameObject.name == prefabName)
                            {
                                alreadyRegistered = true;
                                break;
                            }
                        }

                        if (!alreadyRegistered)
                            spawnablePrefabs.AddObject(prefabNO);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to register {prefabName} in SpawnablePrefabs: {ex.Message}");
                }

                // Organize the prefab in the scene hierarchy to avoid clutter
                NPCPrefabContainer.OrganizePrefab(prefabNO.gameObject, npcType.Name);

                TypeToPrefab[npcType] = prefabNO.gameObject;
                MarkPrefabsConfigured();
                return prefabNO.gameObject;
            }
        }

        private static void InvokeConfigurePrefabWithoutInstance(System.Type npcType, NPCPrefabBuilder builder)
        {
            if (npcType == null || builder == null)
                return;

            // Skip if the type did not override ConfigurePrefab
            MethodInfo configureMethod = npcType.GetMethod("ConfigurePrefab", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (configureMethod == null || configureMethod.DeclaringType == typeof(NPC))
                return;

            NPC tempInstance = null;
            try
            {
                tempInstance = (NPC)FormatterServices.GetUninitializedObject(npcType);
                configureMethod.Invoke(tempInstance, new object[] { builder });
            }
            finally
            {
                tempInstance = null;
            }
        }

        private static string GetPrefabNameForType(System.Type npcType)
        {
            // Avoid path separators, keep name concise, deterministic per type
            string typeName = npcType != null ? npcType.Name : "UnknownNPC";
            return $"S1API_{typeName}";
        }

        /// <summary>
        /// INTERNAL: Creates a wrapper for a network-spawned custom NPC on clients.
        /// Called when a client receives an NPC that was spawned on the server.
        /// </summary>
        internal static NPC? CreateWrapperForNetworkSpawnedNPC(S1NPCs.NPC baseNpc)
        {
            if (baseNpc == null)
                return null;

            try
            {
                // Check if this is a custom S1API NPC by looking for NPCPrefabIdentity or prefab name
                var identity = baseNpc.GetComponent<NPCPrefabIdentity>();
                string prefabName = baseNpc.gameObject.name;
                
                // Remove "(Clone)" suffix if present
                if (prefabName.EndsWith("(Clone)"))
                    prefabName = prefabName.Substring(0, prefabName.Length - 7);

                // Check if it's an S1API prefab
                if (!prefabName.StartsWith("S1API_", StringComparison.Ordinal) && identity == null)
                    return null;

                // Extract type name from prefab name
                string typeName = prefabName.StartsWith("S1API_", StringComparison.Ordinal)
                    ? prefabName.Substring(6) // Remove "S1API_" prefix
                    : null;

                if (string.IsNullOrEmpty(typeName))
                    return null;

                // Find the NPC type in loaded assemblies
                System.Type npcType = null;
                var baseType = typeof(NPC);
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int ai = 0; ai < asms.Length && npcType == null; ai++)
                {
                    var asm = asms[ai];
                    if (asm == baseType.Assembly)
                        continue; // Skip S1API assembly (internal wrappers)

                    System.Type[] types;
                    try { types = asm.GetTypes(); } catch { continue; }
                    for (int ti = 0; ti < types.Length; ti++)
                    {
                        var t = types[ti];
                        if (t == null || t.IsAbstract || !baseType.IsAssignableFrom(t))
                            continue;
                        if (t.Name == typeName)
                        {
                            npcType = t;
                            break;
                        }
                    }
                }

                if (npcType == null)
                    return null;

                // Check if wrapper already exists
                for (int i = 0; i < All.Count; i++)
                {
                    var existing = All[i];
                    if (existing != null && existing.S1NPC == baseNpc)
                        return existing;
                }

                // Create uninitialized instance (avoids constructor which creates new GameObject)
                NPC wrapper = (NPC)FormatterServices.GetUninitializedObject(npcType);
                
                // Use reflection to set readonly fields/properties
                bool s1NpcSet = Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(wrapper, "S1NPC", baseNpc);
                bool isCustomNpcSet = Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(wrapper, "IsCustomNPC", true);
                
                // gameObject is a readonly auto-property, need to find and set its backing field
                bool gameObjectSet = false;
                var allFields = Internal.Utils.ReflectionUtils.GetAllFields(npcType, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                
                for (int fi = 0; fi < allFields.Length; fi++)
                {
                    var field = allFields[fi];
                    // Try compiler-generated backing field name or direct field name
                    if ((field.Name == "<gameObject>k__BackingField" || field.Name == "gameObject") && 
                        field.FieldType == typeof(GameObject))
                    {
                        try
                        {
                            field.SetValue(wrapper, baseNpc.gameObject);
                            gameObjectSet = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning($"CreateWrapperForNetworkSpawnedNPC: Exception setting gameObject backing field '{field.Name}': {ex.Message}");
                        }
                    }
                }
                
                // Fallback: try the property setter if field wasn't found
                if (!gameObjectSet)
                    gameObjectSet = Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(wrapper, "gameObject", baseNpc.gameObject);

                // Validate that critical fields were set
                if (!s1NpcSet)
                {
                    Logger.Warning($"CreateWrapperForNetworkSpawnedNPC: Could not set S1NPC field/property for '{baseNpc.ID}'.");
                    return null;
                }
                if (!gameObjectSet)
                {
                    Logger.Warning($"CreateWrapperForNetworkSpawnedNPC: Could not set gameObject field/property for '{baseNpc.ID}'. Tried backing field and property setter.");
                    return null;
                }

                // Verify the wrapper has the correct references
                if (wrapper.S1NPC != baseNpc)
                {
                    Logger.Warning($"CreateWrapperForNetworkSpawnedNPC: S1NPC field not set correctly for '{baseNpc.ID}'.");
                    return null;
                }
                if (wrapper.gameObject != baseNpc.gameObject)
                {
                    Logger.Warning($"CreateWrapperForNetworkSpawnedNPC: gameObject field not set correctly for '{baseNpc.ID}'.");
                    return null;
                }

                InitializeWrapperStateFromNetworkSpawn(wrapper, baseNpc);

                // Add to All list
                All.Add(wrapper);

                return wrapper;
            }
            catch (Exception ex)
            {
                Logger.Warning($"CreateWrapperForNetworkSpawnedNPC: Exception creating wrapper for '{baseNpc?.ID ?? "<null>"}': {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        private static void InitializeWrapperStateFromNetworkSpawn(NPC wrapper, S1NPCs.NPC baseNpc)
        {
            if (wrapper == null || baseNpc == null)
                return;

            try
            {
                var runtimeAvatar = baseNpc.Avatar ?? baseNpc.gameObject?.GetComponentInChildren<S1AvatarFramework.Avatar>(true);
                wrapper._runtimeAvatar = runtimeAvatar;
                wrapper.Appearance = new NPCAppearance(wrapper, runtimeAvatar);
                wrapper.RestoreRuntimeAvatarAppearance();
                wrapper.RefreshMessagingIcons();

                try
                {
                    var registry = S1NPCs.NPCManager.NPCRegistry;
                    if (registry != null && !registry.Contains(baseNpc))
                        registry.Add(baseNpc);
                }
                catch { }
            }
            catch (Exception ex)
            {
                Logger.Warning($"InitializeWrapperStateFromNetworkSpawn: Failed for '{baseNpc?.ID ?? "<null>"}': {ex.Message}");
            }
        }

        internal void RefreshMessagingIcons()
        {
            try
            {
                Sprite sprite = Icon;
                if (sprite == null)
                    return;

                var convo = S1NPC?.MSGConversation;
                if (convo == null)
                    return;

                var entryRect = convo.entry ?? ResolveConversationRect(convo, "entry");
                var containerRect = ResolveConversationRect(convo, "container");

                TryApplyIconToRect(entryRect, sprite);
                TryApplyIconToRect(containerRect, sprite);
            }
            catch (Exception ex)
            {
                Logger.Warning($"RefreshMessagingIcons failed for '{S1NPC?.ID ?? "<null>"}': {ex.Message}");
            }
        }

        private void TryApplyIconToRect(RectTransform rect, Sprite sprite)
        {
            if (rect == null || sprite == null)
                return;

            ApplyIconToPath(rect, null, sprite);
            ApplyIconToPath(rect, "Icon", sprite);
            ApplyIconToPath(rect, "IconMask/Icon", sprite);
        }

        private static void ApplyIconToPath(RectTransform root, string childPath, Sprite sprite)
        {
            if (root == null || sprite == null)
                return;

            Transform target = string.IsNullOrEmpty(childPath) ? root : root.Find(childPath);
            if (target == null)
                return;

            var image = target.GetComponent<Image>();
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = true;
        }

        private static RectTransform ResolveConversationRect(S1Messaging.MSGConversation convo, string memberName)
        {
            if (convo == null || string.IsNullOrEmpty(memberName))
                return null;

            var value = Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(convo, memberName);
            return value as RectTransform;
        }

        internal static void RegisterSchedulePlanForType(System.Type npcType, System.Collections.Generic.List<IScheduleActionSpec> specs)
        {
            if (npcType == null || specs == null)
                return;
            TypeToSchedulePlan[npcType] = specs;
        }

        /// <summary>
        /// Pre-registers a per-type NPC prefab into FishNet spawnables without creating a live instance.
        /// Should be called on both server and client before any NPC instances are spawned.
        /// </summary>
        public static void PreRegisterPrefabForType(System.Type npcType)
        {
            try
            {
                GetOrCreatePerNpcPrefab(npcType, null);
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Failed to pre-register NPC prefab for {npcType?.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Scans loaded assemblies for subclasses of S1API.Entities.NPC and pre-registers their prefabs.
        /// </summary>
        public static void PreRegisterAllNpcPrefabs()
        {
            try
            {
                // Only pre-register when SpawnablePrefabs is available; otherwise, a warmup will retry shortly
                var nm = InstanceFinder.NetworkManager;
                var spawnables = nm?.SpawnablePrefabs;
                if (spawnables == null)
                    return;

                var baseType = typeof(NPC);
                var baseAssembly = baseType.Assembly;
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int ai = 0; ai < asms.Length; ai++)
                {
                    var asm = asms[ai];
                    Type[] types;
                    try { types = asm.GetTypes(); } catch { continue; }
                    for (int ti = 0; ti < types.Length; ti++)
                    {
                        var t = types[ti];
                        if (t == null || t.IsAbstract)
                            continue;
                        if (baseType.IsAssignableFrom(t))
                        {
                            // Skip internal S1API NPC wrappers; only pre-register mod-defined types
                            if (t.Assembly == baseAssembly)
                                continue;

                            PreRegisterPrefabForType(t);
                        }
                    }
                }

                // Prefabs are configured for this process once registration has been attempted with spawnables present
                MarkPrefabsConfigured();
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] PreRegisterAllNpcPrefabs failed: {ex.Message}");
            }
        }

        internal static void RegisterCustomerDefaultsForType(System.Type npcType, System.Action<CustomerDataBuilder> configure)
        {
            if (npcType == null || configure == null)
                return;
            TypeToCustomerDefaults[npcType] = configure;
        }

        internal static void RegisterCustomerType(System.Type npcType)
        {
            if (npcType == null)
                return;
            CustomerTypes.Add(npcType);
        }

        internal static bool IsCustomerType(System.Type npcType)
        {
            if (npcType == null)
                return false;
            return CustomerTypes.Contains(npcType);
        }

        // Helper accessors for loader-time default application
        internal static bool HasCustomerDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return false;
            return TypeToCustomerDefaults.TryGetValue(npcType, out var cfg) && cfg != null;
        }

        internal static System.Action<CustomerDataBuilder> GetCustomerDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return null;
            TypeToCustomerDefaults.TryGetValue(npcType, out var cfg);
            return cfg;
        }

        internal static S1Economy.CustomerData BuildCustomerDefaultsForType(System.Type npcType)
        {
            var cfg = GetCustomerDefaultsForType(npcType);
            if (cfg == null)
                return null;
            var builder = new CustomerDataBuilder();
            cfg(builder);
            return builder.BuildInternal();
        }

        internal static bool TrySetCustomerDataOnComponent(S1Economy.Customer customerComponent, S1Economy.CustomerData data)
        {
            if (customerComponent == null || data == null)
                return false;
            try
            {
#if MONOMELON
                var field = typeof(S1Economy.Customer).GetField("customerData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                field?.SetValue(customerComponent, data);
                var field2 = typeof(S1Economy.Customer).GetField("currentAffinityData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                field2?.SetValue(customerComponent, data.DefaultAffinityData);
#else
                customerComponent.customerData = data;
                customerComponent.currentAffinityData = data.DefaultAffinityData;
#endif
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryApplyDealerDefaults(S1Economy.Dealer dealerComponent, DealerDataBuilder.DealerConfigData data)
        {
            if (dealerComponent == null || data == null)
                return false;
            try
            {
                string dealerId = string.Empty;
                try
                {
                    dealerId = dealerComponent.ID ?? dealerComponent?.name ?? "<unknown-dealer>";
                }
                catch
                {
                    dealerId = "<unknown-dealer>";
                }

                dealerComponent.SigningFee = data.SigningFee;
                dealerComponent.Cut = data.Cut;
#if MONOMELON
                var dealerTypeField = typeof(S1Economy.Dealer).GetField("DealerType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (dealerTypeField != null)
                {
                    var dealerTypeEnum = Enum.Parse(typeof(S1Economy.EDealerType), data.DealerType.ToString());
                    dealerTypeField.SetValue(dealerComponent, dealerTypeEnum);
                }
#else
                dealerComponent.DealerType = (S1Economy.EDealerType)(int)data.DealerType;
#endif
                dealerComponent.SellInsufficientQualityItems = data.SellInsufficientQualityItems;
                dealerComponent.SellExcessQualityItems = data.SellExcessQualityItems;
                
                // Store Home building reference in NPCPrefabIdentity for resolution in Main scene
                // This runs in Menu scene where buildings aren't available yet
                string buildingNameToStore = null;
                if (data.Home != null)
                {
                    buildingNameToStore = data.Home.Name;
                }
                else if (!string.IsNullOrEmpty(data.HomeName))
                {
                    buildingNameToStore = data.HomeName;
                }

                if (!string.IsNullOrEmpty(buildingNameToStore))
                {
                    // Store building name in NPCPrefabIdentity for deferred resolution
                    var identity = dealerComponent.GetComponent<Internal.Entities.NPCPrefabIdentity>();
                    if (identity != null)
                    {
                        identity.DealerHomeBuildingName = buildingNameToStore;
                    }
                }
                
                // Note: CompletedDealsVariable would need to be set via other means
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"[NPC] TryApplyDealerDefaults: Exception applying dealer defaults: {ex.Message}");
                Logger.Error($"[NPC] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        internal static void RegisterRelationshipDefaultsForType(System.Type npcType, System.Action<NPCRelationshipDataBuilder> configure)
        {
            if (npcType == null || configure == null)
                return;
            TypeToRelationshipDefaults[npcType] = configure;
        }

        internal static void RegisterDealerDefaultsForType(System.Type npcType, System.Action<DealerDataBuilder> configure)
        {
            if (npcType == null || configure == null)
                return;
            TypeToDealerDefaults[npcType] = configure;
        }

        internal static void RegisterDealerType(System.Type npcType)
        {
            if (npcType == null)
                return;
            DealerTypes.Add(npcType);
        }

        internal static bool IsDealerType(System.Type npcType)
        {
            if (npcType == null)
                return false;
            return DealerTypes.Contains(npcType);
        }

        internal static bool HasDealerDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return false;
            return TypeToDealerDefaults.TryGetValue(npcType, out var cfg) && cfg != null;
        }

        internal static System.Action<DealerDataBuilder> GetDealerDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return null;
            TypeToDealerDefaults.TryGetValue(npcType, out var cfg);
            return cfg;
        }

        internal static DealerDataBuilder.DealerConfigData BuildDealerDefaultsForType(System.Type npcType)
        {
            var cfg = GetDealerDefaultsForType(npcType);
            if (cfg == null)
                return null;
            var builder = new DealerDataBuilder();
            cfg(builder);
            return builder.BuildInternal();
        }

        internal static void RegisterRandomInventoryDefaultsForType(System.Type npcType, System.Action<RandomInventoryItemsBuilder> configure)
        {
            if (npcType == null || configure == null)
                return;
            TypeToRandomInventoryDefaults[npcType] = configure;
        }

        internal static bool HasRandomInventoryDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return false;
            return TypeToRandomInventoryDefaults.TryGetValue(npcType, out var cfg) && cfg != null;
        }

        internal static System.Action<RandomInventoryItemsBuilder> GetRandomInventoryDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return null;
            TypeToRandomInventoryDefaults.TryGetValue(npcType, out var cfg);
            return cfg;
        }

        internal static RandomInventoryItemsBuilder.InventoryDefaultsData BuildRandomInventoryDefaultsForType(System.Type npcType)
        {
            var cfg = GetRandomInventoryDefaultsForType(npcType);
            if (cfg == null)
                return null;
            var builder = new RandomInventoryItemsBuilder();
            cfg(builder);
            return builder.BuildInternal();
        }

        internal static void RegisterSpawnPositionForType(System.Type npcType, Vector3 position, Quaternion rotation)
        {
            if (npcType == null)
                return;
            TypeToSpawnPosition[npcType] = (position, rotation);
        }

        private static void MarkPrefabsConfigured()
        {
            _prefabsConfiguredForLocalProcess = true;
        }

#endregion
        
        #region Protected Members

        /// <summary>
        /// A list of text responses you've added to your NPC.
        /// </summary>
        protected readonly System.Collections.Generic.List<Response> Responses = new System.Collections.Generic.List<Response>();

        /// <summary>
        /// Base constructor for a new NPC. Identity is configured via <see cref="ConfigurePrefab"/> using <see cref="NPCPrefabBuilder.WithIdentity"/> and optionally <see cref="NPCPrefabBuilder.WithIcon"/>.
        /// </summary>
        /// <remarks>
        /// Not intended for direct instancing. Create your derived class and let S1API handle instancing.
        /// Identity information (ID, firstName, lastName, icon) must be provided in <see cref="ConfigurePrefab"/> using the builder methods.
        /// </remarks>
        protected NPC()
        {
            IsCustomNPC = true;

            gameObject = InstantiateTemplateInstance(this.GetType(), this);
            gameObject.SetActive(false);

            S1NPCs.NPC? prefabNpc = gameObject.GetComponent<S1NPCs.NPC>();
            if (prefabNpc == null)
                throw new Exception("NPC template is missing the core ScheduleOne.NPCs.NPC component.");

            S1NPC = prefabNpc;

            S1AvatarFramework.Avatar? runtimeAvatar = S1NPC.Avatar ?? gameObject.GetComponentInChildren<S1AvatarFramework.Avatar>(true);
            _runtimeAvatar = runtimeAvatar;

            // EnsureTextMeshProFonts();

            // Read identity from NPCPrefabIdentity component (set by ConfigurePrefab via WithIdentity/WithIcon)
            var identity = gameObject.GetComponent<NPCPrefabIdentity>();
            string id = null;
            string firstName = null;
            string lastName = null;
            Sprite icon = null;

            if (identity != null)
            {
                // On Mono, fields are auto-serialized and available directly
                // On Il2Cpp, fields may not be populated yet, so check registry
#if IL2CPPMELON
                string prefabName = gameObject.name;
                if (NPCPrefabIdentity.TryGetIdentityFromRegistry(prefabName, out string regId, out string regFirstName, out string regLastName, out Sprite regIcon))
                {
                    id = regId;
                    firstName = regFirstName;
                    lastName = regLastName;
                    icon = regIcon;
                }
                else
                {
                    // Fallback to component fields if registry lookup fails
                    id = identity.Id;
                    firstName = identity.FirstName;
                    lastName = identity.LastName;
                    icon = identity.Icon;
                }
#else
                id = identity.Id;
                firstName = identity.FirstName;
                lastName = identity.LastName;
                icon = identity.Icon;
#endif
            }

            // Apply identity values
            if (!string.IsNullOrEmpty(id))
                S1NPC.ID = id;
            if (!string.IsNullOrEmpty(firstName))
                S1NPC.FirstName = firstName;
            if (!string.IsNullOrEmpty(lastName))
                S1NPC.LastName = lastName;
            if (icon != null)
                S1NPC.MugshotSprite = icon;

            // Use default icon if none was set
            if (S1NPC.MugshotSprite == null)
                S1NPC.MugshotSprite = S1DevUtilities.PlayerSingleton<S1ContactApps.ContactsApp>.Instance.AppIcon;

            S1NPC.BakedGUID = Guid.NewGuid().ToString();
            
            EnsureMessageConversationReady(resetDefaults: true);
            InitializeHealthComponent();
            InitializeAwarenessComponent();
            InitializeBehaviourComponents();
            InitializeVisionComponents();
            InitializeInteractables();
            InitializeInventoryComponent();
            InitializeRelationshipData();
            InitializeNetworkBehaviours();

            Appearance = new NPCAppearance(this, _runtimeAvatar);
            RestoreRuntimeAvatarAppearance();

            gameObject.name = S1NPC.FirstName ?? "UnknownNPC";

            // Ensure the base game NPC is added to the registry manually since Awake isn't called when inactive
            if (!S1NPCs.NPCManager.NPCRegistry.Contains(S1NPC))
            {
                S1NPCs.NPCManager.NPCRegistry.Add(S1NPC);
            }

            All.Add(this);
        }

        /// <summary>
        /// Backwards-compatible constructor for non-physical NPCs that provides identity directly via parameters.
        /// This constructor is intended for backwards compatibility with mods that used the old constructor pattern.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This constructor is marked as obsolete. For new code, use the parameterless constructor and configure identity
        /// via <see cref="ConfigurePrefab"/> using <see cref="NPCPrefabBuilder.WithIdentity"/> and optionally <see cref="NPCPrefabBuilder.WithIcon"/>.
        /// </para>
        /// <para>
        /// This constructor is appropriate for non-physical NPCs (where <see cref="IsPhysical"/> returns <c>false</c>) that
        /// don't require prefab configuration. Physical NPCs should use <see cref="ConfigurePrefab"/> for proper network spawn support.
        /// </para>
        /// </remarks>
        /// <param name="id">Unique identifier for the NPC.</param>
        /// <param name="firstName">The first name for the NPC.</param>
        /// <param name="lastName">The last name for the NPC. Can be null.</param>
        /// <param name="icon">The icon sprite for the NPC. Can be null to use default.</param>
        [Obsolete("Use the parameterless constructor and configure identity via ConfigurePrefab with NPCPrefabBuilder.WithIdentity. This constructor is provided for backwards compatibility with non-physical NPCs.")]
        protected NPC(string id, string? firstName, string? lastName, Sprite? icon = null) : this()
        {
            bool hasId = !string.IsNullOrEmpty(id);
            bool hasFirstName = !string.IsNullOrEmpty(firstName);
            bool hasLastName = !string.IsNullOrEmpty(lastName);

            if (hasId)
                S1NPC.ID = id!;
            if (hasFirstName)
                S1NPC.FirstName = firstName!;
            if (hasLastName)
                S1NPC.LastName = lastName!;
            else
                S1NPC.hasLastName = false;
            if (icon != null)
                S1NPC.MugshotSprite = icon;

            var identity = gameObject.GetComponent<NPCPrefabIdentity>();
            if (identity != null)
            {
                if (hasId)
                    identity.Id = id!;
                if (hasFirstName)
                    identity.FirstName = firstName!;
                if (hasLastName)
                    identity.LastName = lastName!;
                if (icon != null)
                    identity.Icon = icon;

                identity.RegisterToStaticCache(gameObject.name);
            }

            if (S1NPC.MugshotSprite == null)
                S1NPC.MugshotSprite = S1DevUtilities.PlayerSingleton<S1ContactApps.ContactsApp>.Instance.AppIcon;

            string displayName = S1NPC.FirstName;
            if (string.IsNullOrEmpty(displayName))
                displayName = hasId ? id! : "UnknownNPC";
            gameObject.name = displayName;

            // Update the message conversation's contact name if it was already created
            if (S1NPC.MSGConversation != null)
            {
                try
                {
                    // Update contactName field/property in MSGConversation
                    string newContactName = S1NPC.fullName;
                    if (string.IsNullOrEmpty(newContactName))
                        newContactName = hasFirstName ? firstName! : (hasId ? id! : "Unknown");
                    
                    Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(S1NPC.MSGConversation, "contactName", newContactName);

                    // Refresh the UI to show the updated name
                    RefreshMessagingIcons();
                    
                    // Update the entry name text if UI exists by calling SetIsKnown with current value
                    var setIsKnownMethod = typeof(S1Messaging.MSGConversation).GetMethod("SetIsKnown", BindingFlags.Public | BindingFlags.Instance);
                    if (setIsKnownMethod != null)
                    {
                        var isKnownValue = Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(S1NPC.MSGConversation, "IsSenderKnown");
                        bool isKnown = isKnownValue is bool known ? known : true;
                        setIsKnownMethod.Invoke(S1NPC.MSGConversation, new object[] { isKnown });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to update MSGConversation contactName for '{S1NPC?.ID ?? "<null>"}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Override to configure NPC components and default behavior before the NPC is spawned.
        /// Called during prefab creation to set up spawn position, customer behavior, relationships, and schedules.
        /// </summary>
        /// <remarks>
        /// Customer, relationship, and schedule configuration must be done here for proper save/load behavior and network compatibility.
        /// Use the builder pattern for fluent configuration. Runtime initialization should be done in <see cref="OnCreated"/> instead.
        /// </remarks>
        /// <param name="builder">Prefab builder for configuring this NPC type.</param>
        protected virtual void ConfigurePrefab(NPCPrefabBuilder builder) { }

        /// <summary>
        /// Called when a text message response is loaded from the save file.
        /// Override to re-attach callbacks to loaded responses.
        /// </summary>
        /// <param name="response">The response that was loaded from save data.</param>
        protected virtual void OnResponseLoaded(Response response) { }

        /// <summary>
        /// Called when the NPC is fully created and spawned. Override for runtime initialization after all components are set up.
        /// Use this to configure appearance, dialogue systems, subscribe to events, enable schedule system, and set basic properties.
        /// </summary>
        /// <remarks>
        /// Called after the NPC is instantiated and all components are initialized. Appearance, dialogue, and schedule setup should be done here rather than in the constructor.
        /// </remarks>
        protected override void OnCreated()
        {
            Appearance.GenerateMugshot();
            RestoreRuntimeAvatarAppearance();
            // Adding a movement component when NPC is created prevents it from disabling
            if (S1NPC.Movement == null)
                S1NPC.Movement = gameObject.GetComponent<S1NPCs.NPCMovement>();

            S1NPC.Movement.enabled = true;
        }

        #endregion

        // Public members intended to be used by modders.
        // Can be used inside your derived class, or outside via instance reference.
        #region Public Members

        /// <summary>
        /// INTERNAL: Tracking for the GameObject associated with this NPC.
        /// Not intended for use by modders!
        /// </summary>
        public GameObject gameObject { get; }

        /// <summary>
        /// The world position of the NPC.
        /// </summary>
        public Vector3 Position
        {
            get => gameObject.transform.position;
            set => S1NPC.Movement.Warp(value);
        }

        /// <summary>
        /// The transform of the NPC.
        /// Please do not set the properties of this transform.
        /// </summary>
        public Transform Transform =>
            gameObject.transform;

        /// <summary>
        /// List of all NPCs within the base game and modded.
        /// </summary>
        public static readonly System.Collections.Generic.List<NPC> All = new System.Collections.Generic.List<NPC>();

        /// <summary>
        /// The first name of this NPC.
        /// </summary>
        public string FirstName
        {
            get => S1NPC.FirstName;
            set => S1NPC.FirstName = value;
        }

        /// <summary>
        /// The last name of this NPC.
        /// </summary>
        public string LastName
        {
            get => S1NPC.LastName;
            set => S1NPC.LastName = value;
        }

        /// <summary>
        /// The full name of this NPC.
        /// If there is no last name, it will just return the first name.
        /// </summary>
        public string FullName =>
            S1NPC.fullName;

        /// <summary>
        /// The unique identifier to assign to this NPC.
        /// Used when saving and loading. Probably other things within the base game code.
        /// </summary>
        public string ID
        {
            get => S1NPC.ID;
            protected set => S1NPC.ID = value;
        }

        /// <summary>
        /// Static NPC ID for this NPC type. Used to resolve connections during prefab configuration
        /// when NPC instances are not yet available. Override this in derived classes to provide
        /// the NPC ID string (e.g., "kyle_cooley", "ludwig_meyer").
        /// </summary>
        /// <remarks>
        /// For built-in NPC wrappers, this should return the ID string that matches the base game NPC.
        /// For custom NPCs, this should return the ID configured via <see cref="NPCPrefabBuilder.WithIdentity"/>.
        /// </remarks>
        public static string? NPCId => null;

        /// <summary>
        /// The icon assigned to this NPC.
        /// </summary>
        public Sprite Icon
        {
            get => S1NPC.MugshotSprite;
            set => S1NPC.MugshotSprite = value;
        }

        /// <summary>
        /// Whether the NPC is currently conscious or not.
        /// </summary>
        public bool IsConscious =>
            S1NPC.IsConscious;

        /// <summary>
        /// Whether the NPC is currently inside a building or not.
        /// </summary>
        public bool IsInBuilding =>
            S1NPC.isInBuilding;

        /// <summary>
        /// Whether the NPC is currently inside a vehicle or not.
        /// </summary>
        public bool IsInVehicle =>
            S1NPC.IsInVehicle;

        /// <summary>
        /// Whether the NPC is currently panicking or not.
        /// </summary>
        public bool IsPanicking =>
            S1NPC.IsPanicked;

        /// <summary>
        /// Whether the NPC is currently unsettled or not.
        /// </summary>
        public bool IsUnsettled =>
            S1NPC.isUnsettled;

        /// <summary>
        /// UNCONFIRMED: Whether the NPC is currently visible to the player or not.
        /// If you confirm this, please let us know so we can update the documentation!
        /// </summary>
        public bool IsVisible =>
            S1NPC.isVisible;

        /// <summary>
        /// Determines if the NPC is visible in the game world. Override as true for physical NPCs with 3D models, movement, and direct interaction.
        /// </summary>
        /// <remarks>
        /// Physical NPCs (<c>true</c>): Visible in world, have collision detection, can move and follow schedules, can be damaged/healed.
        /// Non-physical NPCs (<c>false</c>): Invisible, primarily for messaging and phone contacts, cannot move or be directly interacted with.
        /// </remarks>
        public virtual bool IsPhysical => false;
        
        /// <summary>
        /// Determines if the NPC has dealer functionality. Override as true for NPCs that should be dealers.
        /// </summary>
        /// <remarks>
        /// Dealer NPCs (<c>true</c>): Can manage customers, handle contracts, accept cash payments, and track inventory for sales.
        /// When true, the NPC prefab will use the "Dealer" network prefab instead of "BaseNPC".
        /// Non-dealer NPCs (<c>false</c>): Regular NPCs without dealer-specific functionality.
        /// </remarks>
        public virtual bool IsDealer => false;

        internal void EnsureMessageConversationReady(bool resetDefaults)
        {
            try
            {
                var categories = resetDefaults
                    ? ResetConversationCategoriesToDefaults()
                    : EnsureConversationCategoriesInitialized();

                EnsureMessageConversationInstance(categories);
            }
            catch (Exception ex)
            {
                Logger.Warning($"EnsureMessageConversationReady exception for '{S1NPC?.ID ?? "<null>"}': {ex.Message}");
            }
        }

        private ConversationCategoryList EnsureConversationCategoriesInitialized()
        {
            var categories = S1NPC.ConversationCategories as ConversationCategoryList;

            if (categories == null)
            {
                categories = new ConversationCategoryList();
                S1NPC.ConversationCategories = categories;
            }

            if (categories.Count == 0)
            {
                ResetConversationCategoriesToDefaults(categories);
            }

            return categories;
        }

        private ConversationCategoryList ResetConversationCategoriesToDefaults()
        {
            var categories = S1NPC.ConversationCategories as ConversationCategoryList;

            if (categories == null)
            {
                categories = new ConversationCategoryList();
                S1NPC.ConversationCategories = categories;
            }
            else
            {
                categories.Clear();
            }

            ResetConversationCategoriesToDefaults(categories);
            return categories;
        }

        private void ResetConversationCategoriesToDefaults(ConversationCategoryList categories)
        {
            if (categories == null)
                return;

            if (ShouldUseDealerCategory())
            {
                categories.Add(S1Messaging.EConversationCategory.Dealer);
            }
            else
            {
                categories.Add(S1Messaging.EConversationCategory.Customer);
            }
        }

        private bool ShouldUseDealerCategory()
        {
            bool useDealer = false;

            try
            {
                useDealer = IsDealer;
            }
            catch
            {
            }

            if (!useDealer)
            {
                try
                {
                    useDealer = IsDealerType(GetType());
                }
                catch
                {
                }
            }

            return useDealer;
        }

        private void EnsureMessageConversationInstance(ConversationCategoryList categories)
        {
            if (S1NPC == null)
                return;

            if (S1NPC.MSGConversation == null)
            {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                S1NPC.CreateMessageConversation();
#elif (MONOMELON || MONOBEPINEX)
                MethodInfo createConvoMethod = AccessTools.Method(typeof(S1NPCs.NPC), "CreateMessageConversation");
                createConvoMethod?.Invoke(S1NPC, null);
#endif
                if (S1NPC.MSGConversation == null)
                {
                    Logger.Warning($"EnsureMessageConversationInstance: creation failed for '{S1NPC?.ID ?? "<null>"}'.");
                }
            }

            var convo = S1NPC.MSGConversation;
            if (convo == null)
            {
                Logger.Warning($"EnsureMessageConversationInstance: conversation still null for '{S1NPC?.ID ?? "<null>"}'.");
                return;
            }

            if (categories == null)
            {
                Logger.Warning($"EnsureMessageConversationInstance: categories null for '{S1NPC?.ID ?? "<null>"}'.");
                return;
            }

            try
            {
                convo.SetCategories(categories);
            }
            catch (Exception ex)
            {
                Logger.Warning($"EnsureMessageConversationInstance: failed to apply categories for '{S1NPC?.ID ?? "<null>"}': {ex.Message}");
            }
        }

        private static bool SafeIsServer()
        {
            try
            {
                var nm = InstanceFinder.NetworkManager;
                return nm != null && nm.IsServer;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// How aggressive this NPC is towards others.
        /// </summary>
        public float Aggressiveness
        {
            get => S1NPC.Aggression;
            set => S1NPC.Aggression = value;
        }

        /// <summary>
        /// The region the NPC is associated with.
        /// Note: Not the region they're in currently. Just the region they're designated to.
        /// </summary>
        public Region Region
        {
            get => (Region)S1NPC.Region;
            set
            {
                // Map S1API.Map.Region to base game's EMapRegion safely
                try
                {
                    S1NPC.Region = (S1MapBase.EMapRegion)(int)value;
                }
                catch
                {
                    // ignore
                }
            }
        }

        /// <summary>
        /// UNCONFIRMED: How long the NPC will panic for.
        /// If you confirm this, please let us know so we can update the documentation!
        /// </summary>
        public float PanicDuration
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            get => DefaultPanicDuration;
            set { /* no-op under IL2CPP; constant in base game so non accessible */ }
#else
            get => _panicField != null ? (float)_panicField.GetValue(S1NPC)! : DefaultPanicDuration;
            set { _panicField?.SetValue(S1NPC, value); }
#endif
        }

        /// <summary>
        /// Sets the scale of the NPC.
        /// </summary>
        public float Scale
        {
            get => S1NPC.Scale;
            set => S1NPC.SetScale(value);
        }

        /// <summary>
        /// Whether the NPC is knocked out or not.
        /// </summary>
        public bool IsKnockedOut =>
            S1NPC.Health.IsKnockedOut;

        /// <summary>
        /// UNCONFIRMED: Whether the NPC requires the region unlocked in order to deal to.
        /// If you confirm this, please let us know so we can update the documentation!
        /// </summary>
        public bool RequiresRegionUnlocked
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            get => DefaultRequiresRegionUnlocked;
            set { /* no-op under IL2CPP; constant in base game so non accessible */ }
#else
            get => _requiresRegionUnlockedField != null && (bool)_requiresRegionUnlockedField.GetValue(S1NPC)!;
            set { _requiresRegionUnlockedField?.SetValue(S1NPC, value); }
#endif
        }

        // TODO: Add CurrentBuilding (currently missing NPCEnterableBuilding abstraction)
        // public ??? CurrentBuilding { get; set; }

        /// <summary>
        /// The current vehicle the NPC is occupying, if any.
        /// </summary>
        public LandVehicle? CurrentVehicle =>
            S1NPC.CurrentVehicle != null ? new LandVehicle(S1NPC.CurrentVehicle) : null;

        // TODO: Add Inventory (currently missing NPCInventory abstraction)
        // public ??? Inventory { get; set; }

        /// <summary>
        /// The current health the NPC has.
        /// </summary>
        public float CurrentHealth =>
            S1NPC.Health.Health;

        /// <summary>
        /// The maximum health the NPC has.
        /// </summary>
        public float MaxHealth
        {
            get => S1NPC.Health.MaxHealth;
            set => S1NPC.Health.MaxHealth = value;
        }

        /// <summary>
        /// Whether the NPC is dead or not.
        /// </summary>
        public bool IsDead =>
            S1NPC.Health.IsDead;

        /// <summary>
        /// Whether the NPC is invincible or not.
        /// </summary>
        public bool IsInvincible
        {
            get => S1NPC.Health.Invincible;
            set => S1NPC.Health.Invincible = value;
        }

        /// <summary>
        /// Revives the NPC.
        /// </summary>
        public void Revive() =>
            S1NPC.Health.Revive();

        /// <summary>
        /// Deals damage to the NPC.
        /// </summary>
        /// <param name="amount">The amount of damage to deal.</param>
        public void Damage(int amount)
        {
            if (amount <= 0)
                return;

            S1NPC.Health.TakeDamage(amount, true);
        }

        /// <summary>
        ///  Heals the NPC.
        /// </summary>
        /// <param name="amount">The amount of health to heal.</param>
        public void Heal(int amount)
        {
            if (amount <= 0)
                return;

            float actualHealAmount = Mathf.Min(amount, S1NPC.Health.MaxHealth - S1NPC.Health.Health);
            S1NPC.Health.TakeDamage(-actualHealAmount, false);
        }

        /// <summary>
        /// Kills the NPC.
        /// </summary>
        public void Kill() =>
            S1NPC.Health.TakeDamage(S1NPC.Health.MaxHealth);

        /// <summary>
        /// Causes the NPC to become unsettled.
        /// UNCONFIRMED: Will panic them for PanicDuration amount of time.
        /// If you confirm this, please let us know so we can update the documentation!
        /// </summary>
        /// <param name="duration">Length of time they should stay unsettled.</param>
        public void Unsettle(float duration) =>
            _unsettleMethod.Invoke(S1NPC, new object[] { duration });

        /// <summary>
        /// Smoothly scales the NPC over lerpTime.
        /// </summary>
        /// <param name="scale">The scale you want set.</param>
        /// <param name="lerpTime">The time to scale over.</param>
        public void LerpScale(float scale, float lerpTime) =>
            S1NPC.SetScale(scale, lerpTime);

        /// <summary>
        /// Causes the NPC to become panicked.
        /// </summary>
        public void Panic() =>
            S1NPC.SetPanicked();

        /// <summary>
        /// Causes the NPC to stop panicking, if they are currently.
        /// </summary>
        public void StopPanicking() =>
            _removePanicMethod.Invoke(S1NPC, new object[] { });

        /// <summary>
        /// Knocks the NPC out.
        /// NOTE: Does not work for invincible NPCs.
        /// </summary>
        public void KnockOut() =>
            S1NPC.Health.KnockOut();

        /// <summary>
        /// Tells the NPC to travel to a specific position in world space.
        /// </summary>
        /// <param name="position">The position to travel to.</param>
        public void Goto(Vector3 position) =>
            S1NPC.Movement.SetDestination(position);

        // TODO: Add OnEnterVehicle listener (currently missing LandVehicle abstraction)
        // public event Action OnEnterVehicle { }

        // TODO: Add OnExitVehicle listener (currently missing LandVehicle abstraction)
        // public event Action OnExitVehicle { }

        // TODO: Add OnExplosionHeard listener (currently missing NoiseEvent abstraction)
        // public event Action OnExplosionHeard { }

        // TODO: Add OnGunshotHeard listener (currently missing NoiseEvent abstraction)
        // public event Action OnGunshotHeard { }

        // TODO: Add OnHitByCar listener (currently missing LandVehicle abstraction)
        // public event Action OnHitByCar { }

        // TODO: Add OnNoticedDrugDealing listener (currently missing Player abstraction)
        // public event Action OnNoticedDrugDealing { }

        // TODO: Add OnNoticedGeneralCrime listener (currently missing Player abstraction)
        // public event Action OnNoticedGeneralCrime { }

        // TODO: Add OnNoticedPettyCrime listener (currently missing Player abstraction)
        // public event Action OnNoticedPettyCrime { }

        // TODO: Add OnPlayerViolatingCurfew listener (currently missing Player abstraction)
        // public event Action OnPlayerViolatingCurfew { }

        // TODO: Add OnNoticedSuspiciousPlayer listener (currently missing Player abstraction)
        // public event Action OnNoticedSuspiciousPlayer { }

        /// <summary>
        /// Called when the NPC died.
        /// </summary>
        public event Action OnDeath
        {
            add => EventHelper.AddListener(value, S1NPC.Health.onDie);
            remove => EventHelper.RemoveListener(value, S1NPC.Health.onDie);
        }

        /// <summary>
        /// Called when the NPC's inventory contents change.
        /// </summary>
        public event Action OnInventoryChanged
        {
            add => EventHelper.AddListener(value, S1NPC.Inventory.onContentsChanged);
            remove => EventHelper.RemoveListener(value, S1NPC.Inventory.onContentsChanged);
        }

        /// <summary>
        /// Access to the appearance customization system for visual avatar management.
        /// </summary>
        public NPCAppearance Appearance { get; private set; }

        /// <summary>
        /// Access to the movement system for controlling NPC movement and navigation.
        /// </summary>
        public NPCMovement Movement => new NPCMovement(this);
        
        /// <summary>
        /// The current <see cref="CombatBehaviour"/> instance.
        /// </summary>
        public CombatBehaviour CombatBehaviour => new CombatBehaviour(this);

        /// <summary>
        /// Access to the dialogue system for interactive conversations and dialogue trees.
        /// </summary>
        public NPCDialogue Dialogue => _dialogue ?? (_dialogue = new NPCDialogue(this));

        /// <summary>
        /// Access to the schedule system for movement and activity scheduling.
        /// </summary>
        public NPCSchedule Schedule => _schedule ?? (_schedule = new NPCSchedule(this));

        /// <summary>
        /// Access to the inventory system for item management.
        /// </summary>
        public NPCInventory Inventory => _inventory ?? (_inventory = new NPCInventory(this));

        /// <summary>
        /// Access to the customer behavior system for NPCs that act as business customers.
        /// </summary>
        public NPCCustomer Customer => _customer ?? (_customer = new NPCCustomer(this));

        /// <summary>
        /// Access to the dealer system for NPCs that act as product distributors.
        /// </summary>
        public NPCDealer Dealer => _dealer ?? (_dealer = new NPCDealer(this));

        /// <summary>
        /// Access to the relationship system for social connections and relationships with the player.
        /// </summary>
        public NPCRelationship Relationship => _relationship ?? (_relationship = new NPCRelationship(this));

        /// <summary>
        /// Sends a text message from this NPC to the players.
        /// Supports responses with callbacks for additional logic.
        /// </summary>
        /// <param name="message">The message you want the player to see. Unity rich text is allowed.</param>
        /// <param name="responses">Instances of <see cref="Response"/> to display.</param>
        /// <param name="responseDelay">The delay between when the message is sent and when the player can reply.</param>
        /// <param name="network">Whether this should propagate to all players or not.</param>
        public void SendTextMessage(string message, Response[]? responses = null, float responseDelay = 1f, bool network = true)
        {
            if (S1NPC.MSGConversation == null)
            {
                Logger.Warning($"SendTextMessage: MSGConversation null before send for '{S1NPC.ID}'. Trying to ensure.");
                EnsureMessageConversationReady(resetDefaults: false);
            }

            S1NPC.SendTextMessage(message);
            if (responses == null || responses.Length == 0)
            {
                if (S1NPC.MSGConversation == null)
                {
                    Logger.Warning($"SendTextMessage: Conversation still null after send for '{S1NPC.ID}'.");
                }
                return;
            }

            if (S1NPC.MSGConversation == null)
            {
                Logger.Warning($"SendTextMessage: Unable to show responses because MSGConversation is null for '{S1NPC.ID}'.");
                return;
            }

            S1NPC.MSGConversation.ClearResponses();
            Responses.Clear();

            List<S1Messaging.Response> responsesList = new List<S1Messaging.Response>();

            foreach (Response response in responses)
            {
                Responses.Add(response);
                responsesList.Add(response.S1Response);
            }

            S1NPC.MSGConversation.ShowResponses(
                responsesList,
                responseDelay,
                network
            );
        }

        /// <summary>
        /// Set's whether the text message can be deleted/hidden
        /// </summary>
        public bool ConversationCanBeHidden
        {
            get => S1NPC.ConversationCanBeHidden;
            set => S1NPC.ConversationCanBeHidden = value;
        }

        /// <summary>
        /// Sets an equippable item for the NPC.
        /// </summary>
        /// <param name="assetPath">The asset path to the equippable item. <see cref="Misc"/> can be used here.</param>
        public void SetEquippable(string assetPath) => S1NPC.SetEquippable_Return(assetPath);

        /// <summary>
        /// Gets the instance of an NPC.
        /// Supports base NPCs as well as other mod NPCs.
        /// For base NPCs, <see cref="NPCs"/>.
        /// </summary>
        /// <typeparam name="T">The NPC class to get the instance of.</typeparam>
        /// <returns></returns>
        public static NPC? Get<T>() where T : NPC =>
            All.FirstOrDefault(npc => npc.GetType() == typeof(T)) ?? TryCreateBuiltInWrapper(typeof(T));

        /// <summary>
        /// INTERNAL: Lazily creates built-in NPC wrappers (base-game NPCs) when they haven't been materialized yet.
        /// Avoids instantiating custom mod NPCs to prevent unintended prefab creation during ConfigurePrefab.
        /// </summary>
        /// <param name="npcType">Target NPC wrapper type.</param>
        /// <returns>The wrapper instance if created; otherwise, null.</returns>
        private static NPC? TryCreateBuiltInWrapper(System.Type npcType)
        {
            if (npcType == null || npcType.Assembly != typeof(NPC).Assembly || npcType.IsAbstract)
                return null;

            try
            {
#if (IL2CPPBEPINEX || IL2CPPMELON)
                // Allow non-public constructors on Il2Cpp
                return (NPC?)System.Activator.CreateInstance(npcType, true);
#else
                var ctor = npcType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
                if (ctor != null)
                    return (NPC?)ctor.Invoke(null);
#endif
            }
            catch
            {
                // Swallow exceptions to avoid breaking prefab configuration when base NPCs are not yet available.
            }

            return null;
        }

        #endregion

        // Internal members used by S1API.
        // Please do not attempt to use these members!
        #region Internal Members

        /// <summary>
        /// INTERNAL: Reference to the NPC on the S1 side.
        /// </summary>
        internal readonly S1NPCs.NPC S1NPC;

        /// <summary>
        /// INTERNAL: Whether relationship data has been applied from prefab.
        /// Used to determine when NPC initialization is complete.
        /// </summary>
        internal bool RelationshipDataAppliedFromPrefab => _relationshipDataAppliedFromPrefab;

        /// <summary>
        /// INTERNAL: Constructor used for base game NPCs.
        /// </summary>
        /// <param name="npc">Reference to a base game NPC.</param>
        internal NPC(S1NPCs.NPC npc)
        {
            S1NPC = npc;
            gameObject = npc.gameObject;
            IsCustomNPC = false;
            All.Add(this);
        }

        /// <summary>
        /// INTERNAL: Initializes the responses that have been added / loaded
        /// </summary>
        internal override void CreateInternal()
        {
            // Assign responses to our tracked responses
            if (S1NPC?.MSGConversation != null && S1NPC.MSGConversation.currentResponses != null)
            {
                foreach (S1Messaging.Response s1Response in S1NPC.MSGConversation.currentResponses)
                {
                    Response response = new Response(s1Response) { Label = s1Response.label, Text = s1Response.text };
                    Responses.Add(response);
                    OnResponseLoaded(response);
                }
            }

            base.CreateInternal();
        }

        internal override void SaveInternal(string folderPath, ref List<string> extraSaveables)
        {
            string npcPath = Path.Combine(folderPath, S1NPC.SaveFolderName);
            base.SaveInternal(npcPath, ref extraSaveables);
        }
        #endregion

        // Private members used by the NPC class.
        // Please do not attempt to use these members!
        #region Initialization Helpers

        private void InitializeHealthComponent()
        {
            S1NPC.Health = S1NPC.Health ?? gameObject.GetComponent<S1NPCs.NPCHealth>();
            if (S1NPC.Health == null)
                S1NPC.Health = gameObject.AddComponent<S1NPCs.NPCHealth>();

            if (S1NPC.Health.onDie == null)
                S1NPC.Health.onDie = new UnityEvent();
            if (S1NPC.Health.onKnockedOut == null)
                S1NPC.Health.onKnockedOut = new UnityEvent();

            // S1NPC.Health.Invincible = true;
            S1NPC.Health.MaxHealth = 100f;
        }

        private void InitializeAwarenessComponent()
        {
            if (S1NPC.Awareness == null)
            {
                S1NPC.Awareness = gameObject.GetComponentInChildren<S1NPCs.NPCAwareness>(true);
                if (S1NPC.Awareness == null)
                {
                    GameObject awarenessObject = new GameObject("NPCAwareness");
                    awarenessObject.transform.SetParent(gameObject.transform, false);
                    S1NPC.Awareness = awarenessObject.AddComponent<S1NPCs.NPCAwareness>();
                }
            }

            if (S1NPC.Awareness.onExplosionHeard == null)
                S1NPC.Awareness.onExplosionHeard = new UnityEvent<S1Noise.NoiseEvent>();
            if (S1NPC.Awareness.onGunshotHeard == null)
                S1NPC.Awareness.onGunshotHeard = new UnityEvent<S1Noise.NoiseEvent>();
            if (S1NPC.Awareness.onHitByCar == null)
                S1NPC.Awareness.onHitByCar = new UnityEvent<S1Vehicles.LandVehicle>();
            if (S1NPC.Awareness.onNoticedDrugDealing == null)
                S1NPC.Awareness.onNoticedDrugDealing = new UnityEvent<S1PlayerScripts.Player>();
            if (S1NPC.Awareness.onNoticedGeneralCrime == null)
                S1NPC.Awareness.onNoticedGeneralCrime = new UnityEvent<S1PlayerScripts.Player>();
            if (S1NPC.Awareness.onNoticedPettyCrime == null)
                S1NPC.Awareness.onNoticedPettyCrime = new UnityEvent<S1PlayerScripts.Player>();
            if (S1NPC.Awareness.onNoticedPlayerViolatingCurfew == null)
                S1NPC.Awareness.onNoticedPlayerViolatingCurfew = new UnityEvent<S1PlayerScripts.Player>();
            if (S1NPC.Awareness.onNoticedSuspiciousPlayer == null)
                S1NPC.Awareness.onNoticedSuspiciousPlayer = new UnityEvent<S1PlayerScripts.Player>();

            if (S1NPC.Awareness.Listener == null)
                S1NPC.Awareness.Listener = gameObject.GetComponent<S1Noise.Listener>() ?? gameObject.AddComponent<S1Noise.Listener>();

            if (S1NPC.Responses == null)
            {
                S1NPC.Responses = gameObject.GetComponentInChildren<S1Responses.NPCResponses>(true);
                if (S1NPC.Responses == null)
                {
                    GameObject responsesObject = new GameObject("NPCResponses");
                    responsesObject.transform.SetParent(gameObject.transform, false);
                    S1NPC.Responses = responsesObject.AddComponent<S1Responses.NPCResponses_Civilian>();
                }
            }

            // Ensure civilians get the civilian responses implementation so impacts provoke reactions
            if (!(S1NPC.Responses is S1Responses.NPCResponses_Civilian))
            {
                try
                {
                    var respGO = S1NPC.Responses != null ? S1NPC.Responses.gameObject : gameObject;
                    if (S1NPC.Responses != null)
                        UnityEngine.Object.Destroy(S1NPC.Responses);
                    var civilian = respGO.AddComponent<S1Responses.NPCResponses_Civilian>();
                    S1NPC.Responses = civilian;
                }
                catch { }
            }

            // Always link Awareness.Responses to the valid NPCResponses_Civilian component
            // This ensures the reference is properly set after instantiation and component creation/replacement
            if (S1NPC.Responses is S1Responses.NPCResponses_Civilian validCivilianResponses)
            {
                S1NPC.Awareness.Responses = validCivilianResponses;
            }
        }

        private void InitializeBehaviourComponents()
        {
            if (S1NPC.Behaviour == null)
            {
                GameObject behaviourObject = new GameObject("NPCBehaviour");
                behaviourObject.transform.SetParent(gameObject.transform, false);
                S1NPC.Behaviour = behaviourObject.AddComponent<S1Behaviour.NPCBehaviour>();
            }

            // Ensure NPCActions exists so Responses can trigger behaviours like CallPolice/Face/Combat
            if (S1NPC.Actions == null)
            {
                var existing = S1NPC.GetComponentInChildren<S1NPCs.Actions.NPCActions>(true);
                if (existing == null)
                {
                    GameObject actionsObject = new GameObject("NPCActions");
                    actionsObject.transform.SetParent(gameObject.transform, false);
                    existing = actionsObject.AddComponent<S1NPCs.Actions.NPCActions>();
                }
                S1NPC.Actions = existing;
            }

            if (S1NPC.Behaviour.CoweringBehaviour == null)
            {
                S1Behaviour.CoweringBehaviour existing = S1NPC.Behaviour.GetComponentInChildren<S1Behaviour.CoweringBehaviour>(true);
                if (existing == null)
                {
                    GameObject coweringObject = new GameObject("CowingBehaviour");
                    coweringObject.transform.SetParent(S1NPC.Behaviour.transform, false);
                    existing = coweringObject.AddComponent<S1Behaviour.CoweringBehaviour>();
                }

                S1NPC.Behaviour.CoweringBehaviour = existing;
            }

            if (S1NPC.Behaviour.FleeBehaviour == null)
            {
                S1Behaviour.FleeBehaviour existing = S1NPC.Behaviour.GetComponentInChildren<S1Behaviour.FleeBehaviour>(true);
                if (existing == null)
                {
                    GameObject fleeObject = new GameObject("FleeBehaviour");
                    fleeObject.transform.SetParent(S1NPC.Behaviour.transform, false);
                    existing = fleeObject.AddComponent<S1Behaviour.FleeBehaviour>();
                }

                S1NPC.Behaviour.FleeBehaviour = existing;
            }

            // Ensure other behaviours used by Customer flows exist
            if (S1NPC.Behaviour.GenericDialogueBehaviour == null)
            {
                var existing = S1NPC.Behaviour.GetComponentInChildren<S1Behaviour.GenericDialogueBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("GenericDialogueBehaviour");
                    go.transform.SetParent(S1NPC.Behaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.GenericDialogueBehaviour>();
                }
                S1NPC.Behaviour.GenericDialogueBehaviour = existing;
            }

            if (S1NPC.Behaviour.RequestProductBehaviour == null)
            {
                var existing = S1NPC.Behaviour.GetComponentInChildren<S1Behaviour.RequestProductBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("RequestProductBehaviour");
                    go.transform.SetParent(S1NPC.Behaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.RequestProductBehaviour>();
                }
                S1NPC.Behaviour.RequestProductBehaviour = existing;
            }

            if (S1NPC.Behaviour.CallPoliceBehaviour == null)
            {
                var existing = S1NPC.Behaviour.GetComponentInChildren<S1Behaviour.CallPoliceBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("CallPoliceBehaviour");
                    go.transform.SetParent(S1NPC.Behaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.CallPoliceBehaviour>();
                }
                S1NPC.Behaviour.CallPoliceBehaviour = existing;
            }

            if (S1NPC.Behaviour.CombatBehaviour == null)
            {
                var existing = S1NPC.Behaviour.GetComponentInChildren<S1Combat.CombatBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("CombatBehaviour");
                    go.transform.SetParent(S1NPC.Behaviour.transform, false);
                    existing = go.AddComponent<S1Combat.CombatBehaviour>();
                }
                S1NPC.Behaviour.CombatBehaviour = existing;
            }

            if (S1NPC.Behaviour.StationaryBehaviour == null)
            {
                var existing = S1NPC.Behaviour.GetComponentInChildren<S1Behaviour.StationaryBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("StationaryBehaviour");
                    go.transform.SetParent(S1NPC.Behaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.StationaryBehaviour>();
                }
                S1NPC.Behaviour.StationaryBehaviour = existing;
            }

            if (S1NPC.Behaviour.FaceTargetBehaviour == null)
            {
                var existing = S1NPC.Behaviour.GetComponentInChildren<S1Behaviour.FaceTargetBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("FaceTargetBehaviour");
                    go.transform.SetParent(S1NPC.Behaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.FaceTargetBehaviour>();
                }
                S1NPC.Behaviour.FaceTargetBehaviour = existing;
            }

            if (S1NPC.Behaviour.ConsumeProductBehaviour == null)
            {
                var existing = S1NPC.Behaviour.GetComponentInChildren<S1Behaviour.ConsumeProductBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("ConsumeProductBehaviour");
                    go.transform.SetParent(S1NPC.Behaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.ConsumeProductBehaviour>();
                }
                S1NPC.Behaviour.ConsumeProductBehaviour = existing;
            }

            TryRegisterBehaviourEventLinks();
        }

        private void TryRegisterBehaviourEventLinks()
        {
            try
            {
                var beh = S1NPC.Behaviour;
                if (beh == null)
                    return;

                var behaviours = beh.GetComponentsInChildren<S1Behaviour.Behaviour>(true);

                var addMethod = AccessTools.Method(typeof(S1Behaviour.NPCBehaviour), "AddEnabledBehaviour");
                var removeMethod = AccessTools.Method(typeof(S1Behaviour.NPCBehaviour), "RemoveEnabledBehaviour");

                for (int i = 0; i < behaviours.Length; i++)
                {
                    var b = behaviours[i];
                    if (b == null)
                        continue;

                    try
                    {
                        Action enableAction = () =>
                        {
                            try { addMethod?.Invoke(beh, new object[] { b }); } catch { }
                        };
                        EventHelper.AddListener(enableAction, b.onEnable);
                    }
                    catch { }
                    try
                    {
                        Action disableAction = () =>
                        {
                            try { removeMethod?.Invoke(beh, new object[] { b }); } catch { }
                        };
                        EventHelper.AddListener(disableAction, b.onDisable);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void InitializeVisionComponents()
        {
            if (S1NPC.Awareness == null)
                return;

            if (S1NPC.Awareness.VisionCone == null)
            {
                S1Vision.VisionCone existing = gameObject.GetComponentInChildren<S1Vision.VisionCone>(true);
                if (existing == null)
                {
                    GameObject visionObject = new GameObject("VisionCone");
                    visionObject.transform.SetParent(gameObject.transform, false);
                    existing = visionObject.AddComponent<S1Vision.VisionCone>();
                }

                S1NPC.Awareness.VisionCone = existing;
            }

            // Ensure a broad set of visual states are observed so civilians react like base NPCs
            var dsoi = S1NPC.Awareness.VisionCone.DefaultStatesOfInterest;
            if (dsoi == null)
            {
                dsoi = new List<S1Vision.VisionCone.StateContainer>();
                S1NPC.Awareness.VisionCone.DefaultStatesOfInterest = dsoi;
            }
            if (dsoi.Count == 0)
            {
                S1Vision.EVisualState[] defaults = new S1Vision.EVisualState[]
                {
                    S1Vision.EVisualState.PettyCrime,
                    S1Vision.EVisualState.DrugDealing,
                    S1Vision.EVisualState.Vandalizing,
                    S1Vision.EVisualState.Pickpocketing,
                    S1Vision.EVisualState.DisobeyingCurfew,
                    S1Vision.EVisualState.Wanted,
                    S1Vision.EVisualState.Suspicious,
                    S1Vision.EVisualState.Brandishing,
                    S1Vision.EVisualState.DischargingWeapon
                };
                for (int i = 0; i < defaults.Length; i++)
                {
                    dsoi.Add(new S1Vision.VisionCone.StateContainer { state = defaults[i] });
                }
            }

            if (S1NPC.Awareness.VisionCone.QuestionMarkPopup == null)
            {
                S1WorkspacePopup.WorldspacePopup popup =
                    gameObject.GetComponent<S1WorkspacePopup.WorldspacePopup>() ??
                    gameObject.AddComponent<S1WorkspacePopup.WorldspacePopup>();
                S1NPC.Awareness.VisionCone.QuestionMarkPopup = popup;
            }
        }

        private void InitializeInteractables()
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            if (S1NPC.intObj == null)
            {
                S1Interaction.InteractableObject interactable = gameObject.GetComponentInChildren<S1Interaction.InteractableObject>(true) ??
                    gameObject.AddComponent<S1Interaction.InteractableObject>();
                S1NPC.intObj = interactable;
            }
#elif (MONOMELON || MONOBEPINEX)
            FieldInfo intObjField = AccessTools.Field(typeof(S1NPCs.NPC), "intObj");
            if (intObjField.GetValue(S1NPC) == null)
            {
                S1Interaction.InteractableObject interactable = gameObject.GetComponentInChildren<S1Interaction.InteractableObject>(true) ??
                    gameObject.AddComponent<S1Interaction.InteractableObject>();
                intObjField.SetValue(S1NPC, interactable);
            }
#endif
        }

        private void InitializeInventoryComponent()
        {
            if (S1NPC.Inventory == null)
                S1NPC.Inventory = gameObject.GetComponentInChildren<S1NPCs.NPCInventory>(true) ?? gameObject.AddComponent<S1NPCs.NPCInventory>();

            if (S1NPC.Inventory.PickpocketIntObj == null)
            {
                S1Interaction.InteractableObject? talkInteractable = GetPrimaryInteractable();
                S1Interaction.InteractableObject[] interactables = gameObject.GetComponentsInChildren<S1Interaction.InteractableObject>(true);
                S1Interaction.InteractableObject? pickpocket = interactables.FirstOrDefault(io => io != null && io != talkInteractable);
                if (pickpocket == null)
                    pickpocket = gameObject.AddComponent<S1Interaction.InteractableObject>();

                S1NPC.Inventory.PickpocketIntObj = pickpocket;
            }

            // Apply random inventory defaults if configured
            ApplyRandomInventoryDefaults();
        }

        private void InitializeRelationshipData()
        {
            string npcId = S1NPC?.ID ?? "<null>";
            bool relationDataExisted = S1NPC.RelationData != null;

            if (S1NPC.RelationData == null)
            {
                S1NPC.RelationData = new S1Relation.NPCRelationData();
            }

            // Ensure the relation data is bound to this NPC and initialized
            try
            {
                if (S1NPC.RelationData != null)
                {
                    S1NPC.RelationData.Init(S1NPC);
                }
                else
                {
                    Logger.Warning($"[NPC] InitializeRelationshipData: RelationData is still null after creation attempt for NPC '{npcId}'");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[NPC] InitializeRelationshipData: Exception during Init() for NPC '{npcId}': {ex.Message}");
                Logger.Error($"[NPC] InitializeRelationshipData: Stack trace: {ex.StackTrace}");
                /* ignore: base game will handle in its own lifecycle if not ready */
            }
        }

        private void ApplyRandomInventoryDefaults()
        {
            if (!HasRandomInventoryDefaultsForType(GetType()))
                return;

            try
            {
                string npcId = S1NPC?.ID ?? "<null>";
                var data = BuildRandomInventoryDefaultsForType(GetType());
                if (data == null)
                    return;

                var inventory = S1NPC.Inventory;
                if (inventory == null)
                    return;

                // Apply random cash configuration
                if (data.RandomCashMin.HasValue || data.RandomCashMax.HasValue)
                {
                    inventory.RandomCash = true;
                    if (data.RandomCashMin.HasValue)
                        inventory.RandomCashMin = data.RandomCashMin.Value;
                    if (data.RandomCashMax.HasValue)
                        inventory.RandomCashMax = data.RandomCashMax.Value;
                }

                // Apply ClearInventoryEachNight setting
                if (data.ClearInventoryEachNight.HasValue)
                    inventory.ClearInventoryEachNight = data.ClearInventoryEachNight.Value;

                // Apply startup items
                // Always insert items directly and clear StartupItems immediately to prevent Awake from processing them
                // This ensures items are only inserted once, regardless of when ApplyRandomInventoryDefaults() runs
                if (data.StartupItems != null && data.StartupItems.Count > 0)
                {
                    var startupItemsList = new List<S1Items.ItemDefinition>();
                    foreach (var itemId in data.StartupItems)
                    {
                        var def = S1Registry.GetItem(itemId);
                        if (def != null)
                            startupItemsList.Add(def);
                    }

                    if (startupItemsList.Count > 0)
                    {
                        // Check if Awake has already run (slots exist)
                        bool slotsExist = inventory.ItemSlots != null && inventory.ItemSlots.Count > 0;
                        
                        if (slotsExist)
                        {
                            // Awake has run, insert items directly
                            int insertedCount = 0;
                            foreach (var itemDef in startupItemsList)
                            {
                                try
                                {
                                    var itemInstance = itemDef.GetDefaultInstance();
                                    if (itemInstance != null)
                                    {
                                        inventory.InsertItem(itemInstance, network: false);
                                        insertedCount++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Warning($"[NPC] ApplyRandomInventoryDefaults: '{npcId}' failed to insert startup item {itemDef.ID}: {ex.Message}");
                                }
                            }
                            
                            // Clear StartupItems to prevent Awake from processing them again
#if (IL2CPPMELON)
                            inventory.StartupItems = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<S1Items.ItemDefinition>(0);
#else
                            inventory.StartupItems = Array.Empty<S1Items.ItemDefinition>();
#endif
                        }
                        else
                        {
                            // Awake hasn't run yet, set StartupItems for Awake to process
                            // But first check if StartupItems is already set to avoid overwriting
                            bool startupItemsAlreadySet = false;
                            try
                            {
                                if (inventory.StartupItems != null)
                                {
#if (IL2CPPMELON)
                                    var il2cppArray = inventory.StartupItems as Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<S1Items.ItemDefinition>;
                                    startupItemsAlreadySet = il2cppArray != null && il2cppArray.Length > 0;
#else
                                    var array = inventory.StartupItems as S1Items.ItemDefinition[];
                                    startupItemsAlreadySet = array != null && array.Length > 0;
#endif
                                }
                            }
                            catch
                            {
                                startupItemsAlreadySet = false;
                            }

                            if (!startupItemsAlreadySet)
                            {
#if (IL2CPPMELON)
                                inventory.StartupItems = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<S1Items.ItemDefinition>(startupItemsList.ToArray());
#else
                                inventory.StartupItems = startupItemsList.ToArray();
#endif
                            }
                        }
                    }
                    else
                    {
                        Logger.Warning($"[NPC] ApplyRandomInventoryDefaults: '{npcId}' had StartupItems definitions but none resolved from registry.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[NPC] ApplyRandomInventoryDefaults: '{S1NPC?.ID ?? GetType().Name}' failed with exception: {ex.Message}");
                Logger.Warning($"[NPC] ApplyRandomInventoryDefaults: Stack trace: {ex.StackTrace}");
            }
        }

        private S1Interaction.InteractableObject? GetPrimaryInteractable()
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            return S1NPC.intObj;
#elif (MONOMELON || MONOBEPINEX)
            FieldInfo intObjField = AccessTools.Field(typeof(S1NPCs.NPC), "intObj");
            return intObjField.GetValue(S1NPC) as S1Interaction.InteractableObject;
#else
            return null;
#endif
        }

        private void InitializeNetworkBehaviours()
        {
            NetworkBehaviour[] behaviours = gameObject.GetComponentsInChildren<NetworkBehaviour>(true);
            foreach (NetworkBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;

                try
                {
                    // Defer network initialization to FishNet's spawn process. Do not initialize manually.
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to initialize network behaviour {behaviour.GetType().Name}: {ex.Message}");
                }
            }
        }

        private void RestoreRuntimeAvatarAppearance()
        {
            if (_runtimeAvatar == null)
                return;

            S1NPC.Avatar = _runtimeAvatar;
            Appearance.ApplyToAvatar(_runtimeAvatar);
        }

#endregion

        #region Private Members

        internal readonly bool IsCustomNPC;

        private static readonly float DefaultPanicDuration = 20f;
        private static readonly bool DefaultRequiresRegionUnlocked = true;
#if (MONOMELON || MONOBEPINEX)
        private readonly FieldInfo _panicField = AccessTools.Field(typeof(S1NPCs.NPC), "PANIC_DURATION");
        private readonly FieldInfo _requiresRegionUnlockedField = AccessTools.Field(typeof(S1NPCs.NPC), "RequiresRegionUnlocked");
#else
        private readonly FieldInfo _panicField = null;
        private readonly FieldInfo _requiresRegionUnlockedField = null;
#endif

        private readonly MethodInfo _unsettleMethod = AccessTools.Method(typeof(S1NPCs.NPC), "SetUnsettled");
        private readonly MethodInfo _removePanicMethod = AccessTools.Method(typeof(S1NPCs.NPC), "RemovePanicked");

        private NPCDialogue _dialogue;
        private NPCSchedule _schedule;
        private NPCInventory _inventory;
        private NPCCustomer _customer;
        private NPCDealer _dealer;
        private NPCRelationship _relationship;
        private bool _wasLoadedFromSave;
        private S1Relation.NPCRelationData.EUnlockType? _loadedUnlockType;
        private bool _relationshipDataAppliedFromPrefab;

        private void MarkLoadedFromSave()
        {
            _wasLoadedFromSave = true;
        }


        /// <summary>
        /// Spawns this NPC's instance on the server using FishNet so it is networked.
        /// No-ops on clients.
        /// </summary>
        private void TrySpawnNetworkInstance()
        {
            try
            {
                var nm = InstanceFinder.NetworkManager;
                if (nm != null && !nm.IsServer)
                    return;

                NetworkObject no = gameObject.GetComponent<NetworkObject>() ?? gameObject.AddComponent<NetworkObject>();
                NPCNetworkBootstrap.RegisterPendingNetworkSpawn(this, no, 0.3f, 0.6f);
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Failed to queue NPC network spawn: {ex.Message}");
            }
        }

        internal void PrepareForNetworkSpawn()
        {
            try
            {
                var customer = gameObject.GetComponent<S1Economy.Customer>();
                if (customer != null)
                    Customer.EnsureCustomer();
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Failed to prepare customer data before spawn: {ex.Message}");
            }
        }

        internal void FinalizeNetworkSpawn()
        {
            try
            {
                // Ensure NPCAwareness.Responses reference is valid after spawn
                // Network spawning can sometimes break component references
                if (S1NPC.Awareness != null && S1NPC.Responses is S1Responses.NPCResponses_Civilian validResponses)
                {
                    S1NPC.Awareness.Responses = validResponses;
                }

                // Always set visibility locally first (for host/client consistency)
                S1NPC.SetVisible(IsPhysical, networked: false);
                
                // If we're the server, also broadcast to clients via RPC after a delay
                // This ensures the NPC is fully spawned before the RPC is sent
                if (InstanceFinder.IsServer)
                {
                    MelonCoroutines.Start(DelayedVisibilityRPC(IsPhysical));
                }

                // If this prefab included a Customer, ensure it's initialized; otherwise, respect non-customer NPCs
                try
                {
                    if (IsCustomNPC)
                    {
                        var hasCustomer = gameObject.GetComponent<S1Economy.Customer>() != null;
                        if (hasCustomer)
                        {
                            // Customer.EnsureCustomer();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to ensure Customer on NPC: {ex.Message}");
                }

                // If this NPC type was registered as a dealer, ensure dealer initialization and category badge
                try
                {
                    if (IsCustomNPC && IsDealerType(GetType()))
                    {
                        Dealer.EnsureDealer();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to ensure Dealer on NPC: {ex.Message}");
                }

                // Apply any planned schedule specs for this NPC type now that the instance exists
                try
                {
                    var t = GetType();
                    if (TypeToSchedulePlan.TryGetValue(t, out var planned) && planned != null && planned.Count > 0)
                    {
                        for (int i = 0; i < planned.Count; i++)
                        {
                            var spec = planned[i];
                            if (spec != null)
                            {
                                try
                                {
                                    spec.ApplyTo(Schedule);
                                }
                                catch (Exception specEx)
                                {
                                    Logger.Error($"Failed to apply schedule spec {i} ({spec.GetType().Name}) for NPC type {t.Name}: {specEx.Message}");
                                    Logger.Error($"Stack trace: {specEx.StackTrace}");
                                }
                            }
                        }
                        Schedule.InitializeActions();
                        Schedule.EnforceState();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to apply planned schedule for NPC type {GetType().Name}: {ex.Message}");
                    Logger.Error($"Stack trace: {ex.StackTrace}");
                }

                // Apply per-type relationship defaults after base fields are present, unless loaded from save
                // Also preserve unlock state if NPC is already unlocked (might have been loaded from save)
                bool relationDataExists = S1NPC.RelationData != null;
                
                // Check if relationship data appears to have been loaded from save (unlocked or non-default delta)
                // This handles the case where load happens after FinalizeNetworkSpawn but before it runs
                bool appearsLoadedFromSave = false;
                if (relationDataExists)
                {
                    bool isUnlocked = S1NPC.RelationData.Unlocked;
                    float delta = S1NPC.RelationData.RelationDelta;
                    
                    // If NPC is unlocked or delta is not default (2.0), it likely came from save data
                    // This prevents defaults from overwriting loaded relationship data
                    appearsLoadedFromSave = isUnlocked || (Math.Abs(delta - S1Relation.NPCRelationData.DEFAULT_RELATION_DELTA) > 0.01f);
                }
                
                if (!_wasLoadedFromSave && !appearsLoadedFromSave)
                {
                    try
                    {
                        // First, try to apply relationship data from NPCPrefabIdentity (prefab-level, takes precedence)
                        var identity = gameObject.GetComponent<NPCPrefabIdentity>();
                        bool appliedFromPrefab = false;
                        if (identity != null)
                        {
                            var rel = S1NPC.RelationData;
                            if (rel != null)
                            {
                                bool alreadyUnlocked = rel.Unlocked;
                                identity.ApplyRelationshipDataTo(S1NPC, preserveUnlockState: alreadyUnlocked);
                                appliedFromPrefab = true;
                                _relationshipDataAppliedFromPrefab = true;
                                // Verify unlock state wasn't accidentally overwritten
                                if (alreadyUnlocked && !rel.Unlocked)
                                {
                                    Logger.Warning($"[NPC] FinalizeNetworkSpawn: WARNING - Unlock state was lost for NPC '{S1NPC.ID}' after applying prefab defaults. Restoring...");
                                    var unlockType = _loadedUnlockType ?? S1Relation.NPCRelationData.EUnlockType.DirectApproach;
                                    rel.Unlock(unlockType, notify: false);
                                }
                            }
                            else
                            {
                                Logger.Warning($"[Relationship Data] FinalizeNetworkSpawn: RelationData is null for NPC '{S1NPC.ID}'");
                            }
                        }
                        
                        // If no prefab-level data was applied, fall back to type-based defaults
                        if (!appliedFromPrefab)
                        {
                            var t = GetType();
                            bool hasDefaults = TypeToRelationshipDefaults.TryGetValue(t, out var relCfg) && relCfg != null;
                            
                            if (hasDefaults)
                            {
                                var builder = new NPCRelationshipDataBuilder();
                                relCfg(builder);
                                var rel = S1NPC.RelationData;
                                if (rel != null)
                                {
                                    // Preserve unlock state if NPC is already unlocked (may have been loaded from save)
                                    // This prevents overwriting unlock state if FinalizeNetworkSpawn runs after load
                                    bool alreadyUnlocked = rel.Unlocked;
                                    builder.ApplyTo(rel, S1NPC, preserveUnlockState: alreadyUnlocked);
                                    _relationshipDataAppliedFromPrefab = true; // Mark as applied even if from type defaults
                                    // Verify unlock state wasn't accidentally overwritten
                                    if (alreadyUnlocked && !rel.Unlocked)
                                    {
                                        Logger.Warning($"[NPC] FinalizeNetworkSpawn: WARNING - Unlock state was lost for NPC '{S1NPC.ID}' after applying defaults. Restoring...");
                                        // Restore unlock state using stored unlock type, or default to DirectApproach
                                        var unlockType = _loadedUnlockType ?? S1Relation.NPCRelationData.EUnlockType.DirectApproach;
                                        rel.Unlock(unlockType, notify: false);
                                    }
                                }
                                else
                                {
                                    Logger.Warning($"[NPC] FinalizeNetworkSpawn: RelationData is null for NPC '{S1NPC.ID}' - cannot apply defaults!");
                                }
                            }
                            else
                            {
                                // No relationship defaults to apply, mark as complete
                                _relationshipDataAppliedFromPrefab = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[NPC] FinalizeNetworkSpawn: Failed to apply relationship defaults for NPC '{S1NPC.ID}': {ex.Message}");
                        Logger.Error($"[NPC] FinalizeNetworkSpawn: Stack trace: {ex.StackTrace}");
                    }
                }
                else
                {
                    // NPC was loaded from save, relationship data is already initialized, mark as complete
                    _relationshipDataAppliedFromPrefab = true;
                }

                // Note: Random inventory defaults are applied in InitializeInventoryComponent, not here
                // to avoid duplicate item insertion when StartupItems is processed by NPCInventory.Awake

                // Apply spawn position for this NPC type (always applied, regardless of save state)
                try
                {
                    var t = GetType();
                    if (TypeToSpawnPosition.TryGetValue(t, out var spawnData))
                    {
                        Position = spawnData.position;
                        Transform.rotation = spawnData.rotation;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to apply spawn position: {ex.Message}");
                }

                // Check if all custom NPCs are now ready (finalized)
                // This sets the CustomNpcsReady flag once all custom NPCs have been spawned and finalized
                CheckAndSetCustomNpcsReady();
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Failed to finalize NPC after spawn: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if all custom NPCs have been finalized and sets the CustomNpcsReady flag.
        /// This is called from FinalizeNetworkSpawn to signal when all custom NPCs are ready.
        /// </summary>
        private static void CheckAndSetCustomNpcsReady()
        {
            // If already ready, no need to check again
            if (Internal.Patches.NPCPatches.CustomNpcsReady)
                return;

            try
            {
                // Check if all custom NPC types have been instantiated
                var allCustomNpcs = All.Where(n => n.IsCustomNPC).ToList();

                // If there are no custom NPCs, nothing to wait for
                if (allCustomNpcs.Count == 0)
                    return;

                // Get all custom NPC types that should exist
                var customNpcTypes = Internal.Utils.ReflectionUtils.GetDerivedClasses<NPC>()
                    .Where(t => t != null && !t.IsAbstract && t.Assembly != typeof(NPC).Assembly)
                    .ToList();

                // Check if all custom NPC types have at least one instance
                bool allTypesInstantiated = customNpcTypes.All(type =>
                    allCustomNpcs.Any(npc => npc.GetType() == type)
                );

                if (allTypesInstantiated)
                    Internal.Patches.NPCPatches.CustomNpcsReady = true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"[NPC] Failed to check CustomNpcsReady status: {ex.Message}");
            }
        }

        /// <summary>
        /// Coroutine to send visibility RPC after a delay to ensure the NPC is fully spawned.
        /// </summary>
        private IEnumerator DelayedVisibilityRPC(bool isPhysical)
        {
            // Wait a frame to ensure the NPC is fully initialized and spawned
            yield return null;
            
            // Additional small delay to ensure network spawn is complete
            yield return new WaitForSeconds(0.1f);
            
            try
            {
                if (S1NPC != null && S1NPC.gameObject != null)
                {
                    // Broadcast visibility to clients via RPC
                    S1NPC.SetVisible(isPhysical, networked: true);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Failed to send visibility RPC for NPC '{S1NPC?.ID}': {ex.Message}");
            }
        }

        // Removed: component index hack. FishNet assigns NetworkBehaviour indices at spawn based on
        // the behaviours present on the NetworkObject. Forcing ComponentIndex causes mismatches.

#endregion

        /// <summary>
        /// Pre-creates an <see cref="S1NPCs.NPCScheduleManager"/> and all non-abstract <see cref="S1NPCsSchedules.NPCAction"/>s
        /// under the provided prefab root so FishNet assigns stable NetworkBehaviour indices at spawn.
        /// All created action GameObjects are inactive by default; mods can enable/configure them later.
        /// </summary>
        private static void EnsureScheduleActionsOnPrefab(GameObject prefabRoot)
        {
            if (prefabRoot == null)
                return;

            // Ensure manager container
            S1NPCs.NPCScheduleManager existingMgr = prefabRoot.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
            if (existingMgr == null)
            {
                GameObject mgrGo = new GameObject("NPCSchedule");
                mgrGo.transform.SetParent(prefabRoot.transform, false);
                existingMgr = mgrGo.AddComponent<S1NPCs.NPCScheduleManager>();
            }

            // Collect action types via reflection when possible
            System.Collections.Generic.List<S1Type> actionTypes = new System.Collections.Generic.List<S1Type>();
#if (IL2CPPBEPINEX || IL2CPPMELON)
            S1Type baseType = Il2CppType.Of<S1NPCsSchedules.NPCAction>();
#else
            S1Type baseType = typeof(S1NPCsSchedules.NPCAction);
#endif
            try
            {
                var asm = baseType.Assembly;
                if (asm != null)
                {
                    var types = asm.GetTypes();
                    for (int i = 0; i < types.Length; i++)
                    {
                        S1Type t = types[i];
                        if (t == null)
                            continue;
                        if (t.IsAbstract)
                            continue;
                        if (baseType.IsAssignableFrom(t))
                            actionTypes.Add(t);
                    }
                }
            }
            catch
            {
                // Fallback: known concrete action types by simple names in the schedules namespace
                string ns = baseType.Namespace;
                string[] known = new string[]
                {
                    "NPCSignal_WalkToLocation",
                    "NPCSignal_WaitForDelivery",
                    "NPCSignal_UseVendingMachine",
                    "NPCSignal_UseATM",
                    "NPCSignal_HandleDeal",
                    "NPCSignal_DriveToCarPark",
                    "NPCEvent_StayInBuilding",
                    "NPCEvent_Sit",
                    "NPCEvent_LocationDialogue",
                    "NPCEvent_LocationBasedAction",
                    "NPCEvent_Conversate"
                };
                for (int i = 0; i < known.Length; i++)
                {
                    string full = string.IsNullOrEmpty(ns) ? known[i] : (ns + "." + known[i]);
#if (IL2CPPBEPINEX || IL2CPPMELON)
                    S1Type t = Il2CppSystem.Type.GetType(full);
#else
                    S1Type t = System.Type.GetType(full);
#endif
                    if (t != null && !t.IsAbstract && baseType.IsAssignableFrom(t))
                        actionTypes.Add(t);
                }
            }

            // Add one inactive instance of each action type if not already present
            for (int i = 0; i < actionTypes.Count; i++)
            {
                S1Type t = actionTypes[i];
                if (t == null)
                    continue;

                var existing = existingMgr.GetComponentInChildren(t, true);
                if (existing != null)
                    continue;

                GameObject go = new GameObject(t.Name);
                go.transform.SetParent(existingMgr.transform, false);
                var comp = go.AddComponent(t);

                // Best-effort wire internal references so actions have context even while inactive
                try
                {
#if MONOMELON
                    var npcField = t.GetField("npc", BindingFlags.NonPublic | BindingFlags.Instance);
                    var schedField = t.GetField("schedule", BindingFlags.NonPublic | BindingFlags.Instance);
#else
                    var npcField = t.GetField("npc", Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance);
                    var schedField = t.GetField("schedule", Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance);
#endif
                    var baseNpc = prefabRoot.GetComponent<S1NPCs.NPC>();
                    
                    npcField?.SetValue(comp, baseNpc);
                    schedField?.SetValue(comp, existingMgr);
                }
                catch { }
                go.SetActive(false);
            }
        }
    }
}
