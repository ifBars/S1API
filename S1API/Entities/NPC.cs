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
using S1API.Entities.Interfaces;
using S1API.Entities.Schedule;
using S1API.Entities.Customer;
using S1API.Entities.Relation;
using S1API.Internal;
using S1API.Internal.Abstraction;
using S1API.Map;
using S1API.Messaging;
using S1API.Vehicles;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace S1API.Entities
{
    /// <summary>
    /// An abstract class intended to be derived from for creating custom NPCs in the game.
    /// </summary>
    public abstract class NPC : Saveable, IEntity, IHealth
    {
        // Protected members intended to be used by modders.
        // Intended to be used from within the class / derived classes ONLY.
        private static readonly System.Collections.Generic.Dictionary<System.Type, GameObject> TypeToPrefab = new System.Collections.Generic.Dictionary<System.Type, GameObject>();
        private static readonly object TemplateLoadLock = new object();
        private static readonly System.Collections.Generic.Dictionary<System.Type, System.Collections.Generic.List<IScheduleActionSpec>> TypeToSchedulePlan = new System.Collections.Generic.Dictionary<System.Type, System.Collections.Generic.List<IScheduleActionSpec>>();
        private static readonly System.Collections.Generic.Dictionary<System.Type, System.Action<CustomerDataBuilder>> TypeToCustomerDefaults = new System.Collections.Generic.Dictionary<System.Type, System.Action<CustomerDataBuilder>>();
        internal static readonly System.Collections.Generic.Dictionary<System.Type, System.Action<NPCRelationshipDataBuilder>> TypeToRelationshipDefaults = new System.Collections.Generic.Dictionary<System.Type, System.Action<NPCRelationshipDataBuilder>>();
        private static readonly System.Collections.Generic.Dictionary<System.Type, (Vector3 position, Quaternion rotation)> TypeToSpawnPosition = new System.Collections.Generic.Dictionary<System.Type, (Vector3, Quaternion)>();
        private static readonly System.Collections.Generic.HashSet<System.Type> CustomerTypes = new System.Collections.Generic.HashSet<System.Type>();
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

                // Prefer a spawnable prefab provided by the base game (e.g., "BaseNPC").
                var nm = InstanceFinder.NetworkManager;
                if (nm == null)
                    throw new Exception("NetworkManager not found when resolving BaseNPC prefab.");

                PrefabObjects spawnablePrefabs = nm.SpawnablePrefabs;
                if (spawnablePrefabs == null)
                    throw new Exception("SpawnablePrefabs not available on NetworkManager.");

                NetworkObject chosen = null;
                int count = spawnablePrefabs.GetObjectCount();
                for (int i = 0; i < count; i++)
                {
                    NetworkObject obj = spawnablePrefabs.GetObject(true, i);
                    if (obj != null && obj.gameObject != null && obj.gameObject.name == "BaseNPC")
                    {
                        chosen = obj;
                        break;
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
                    throw new Exception("Failed to locate a suitable NPC spawnable prefab (BaseNPC or any with S1NPCs.NPC).");

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
                        // If an NPC component exists and was registered, remove it from the registry immediately
                        var npcComp = prefabNO.gameObject.GetComponent<S1NPCs.NPC>();
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
                    Debug.LogWarning($"[S1API] Failed to register {prefabName} in SpawnablePrefabs: {ex.Message}");
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
                Debug.LogWarning($"[S1API] Failed to pre-register NPC prefab for {npcType?.Name}: {ex.Message}");
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
                Debug.LogWarning($"[S1API] PreRegisterAllNpcPrefabs failed: {ex.Message}");
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

        internal static void RegisterRelationshipDefaultsForType(System.Type npcType, System.Action<NPCRelationshipDataBuilder> configure)
        {
            if (npcType == null || configure == null)
                return;
            TypeToRelationshipDefaults[npcType] = configure;
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
        /// Base constructor for a new NPC.
        /// Not intended for instancing your NPC!
        /// Instead, create your derived class and let S1API handle instancing.
        /// </summary>
        /// <param name="id">Unique identifier for your NPC.</param>
        /// <param name="firstName">The first name for your NPC.</param>
        /// <param name="lastName">The last name for your NPC.</param>
        /// <param name="icon">The icon for your NPC for messages, realationships, etc.</param>
        protected NPC(
            string id,
            string? firstName,
            string? lastName,
            Sprite? icon = null
            )
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

            S1NPC.FirstName = firstName;
            S1NPC.LastName = lastName;
            S1NPC.ID = id;
            S1NPC.MugshotSprite = icon ?? S1DevUtilities.PlayerSingleton<S1ContactApps.ContactsApp>.Instance.AppIcon;
            S1NPC.BakedGUID = Guid.NewGuid().ToString();

            if (S1NPC.ConversationCategories == null)
                S1NPC.ConversationCategories = new List<S1Messaging.EConversationCategory>();
            else
                S1NPC.ConversationCategories.Clear();
            S1NPC.ConversationCategories.Add(S1Messaging.EConversationCategory.Customer);

#if (IL2CPPMELON || IL2CPPBEPINEX)
            S1NPC.CreateMessageConversation();
#elif (MONOMELON || MONOBEPINEX)
            MethodInfo createConvoMethod = AccessTools.Method(typeof(S1NPCs.NPC), "CreateMessageConversation");
            createConvoMethod.Invoke(S1NPC, null);
#endif

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
        /// Override to declare components your NPC prefab must contain before network spawn.
        /// Use <see cref="NPCPrefabBuilder"/> to add Customer, ScheduleManager and pre-create actions.
        /// Default does nothing.
        /// </summary>
        /// <param name="builder">Prefab builder for this NPC type.</param>
        protected virtual void ConfigurePrefab(NPCPrefabBuilder builder) { }

        /// <summary>
        /// Called when a response is loaded from the save file.
        /// Override this method for attaching your callbacks to your methods.
        /// </summary>
        /// <param name="response">The response that was loaded.</param>
        protected virtual void OnResponseLoaded(Response response) { }

        /// <summary>
        /// Override OnCreated to create the Mugshot for the NPC
        /// </summary>
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
        /// Override this as true to make your NPC visible in the world.
        /// </summary>
        protected virtual bool IsPhysical => false;
        
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
        /// The current <see cref="NPCAppearance"/> instance.
        /// </summary>
        public NPCAppearance Appearance { get; private set; }

        /// <summary>
        /// The current <see cref="NPCMovement"/> instance.
        /// </summary>
        public NPCMovement Movement => new NPCMovement(this);

        /// <summary>
        /// The current <see cref="NPCDialogue"/> instance.
        /// </summary>
        public NPCDialogue Dialogue => _dialogue ?? (_dialogue = new NPCDialogue(this));

        /// <summary>
        /// The current <see cref="NPCSchedule"/> instance.
        /// </summary>
        public NPCSchedule Schedule => _schedule ?? (_schedule = new NPCSchedule(this));

        /// <summary>
        /// The current <see cref="NPCInventory"/> instance.
        /// </summary>
        public NPCInventory Inventory => _inventory ?? (_inventory = new NPCInventory(this));

        /// <summary>
        /// The current <see cref="NPCCustomer"/> instance.
        /// </summary>
        public NPCCustomer Customer => _customer ?? (_customer = new NPCCustomer(this));

        /// <summary>
        /// The current <see cref="NPCRelationship"/> instance.
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
            S1NPC.SendTextMessage(message);
            if (responses == null || responses.Length == 0)
                return;

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
        /// Gets the instance of an NPC.
        /// Supports base NPCs as well as other mod NPCs.
        /// For base NPCs, <see cref="NPCs"/>.
        /// </summary>
        /// <typeparam name="T">The NPC class to get the instance of.</typeparam>
        /// <returns></returns>
        public static NPC? Get<T>() where T : NPC =>
            All.FirstOrDefault(npc => npc.GetType() == typeof(T));

        #endregion

        // Internal members used by S1API.
        // Please do not attempt to use these members!
        #region Internal Members

        /// <summary>
        /// INTERNAL: Reference to the NPC on the S1 side.
        /// </summary>
        internal readonly S1NPCs.NPC S1NPC;

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
            foreach (S1Messaging.Response s1Response in S1NPC.MSGConversation.currentResponses)
            {
                Response response = new Response(s1Response) { Label = s1Response.label, Text = s1Response.text };
                Responses.Add(response);
                OnResponseLoaded(response);
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

            if (S1NPC.Awareness.Responses == null && S1NPC.Responses is S1Responses.NPCResponses_Civilian civilianResponses)
                S1NPC.Awareness.Responses = civilianResponses;
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

            // Guarantee slots exist to support capacity checks and insertions
            try
            {
                if (S1NPC.Inventory.ItemSlots == null || S1NPC.Inventory.ItemSlots.Count == 0)
                {
                    for (int i = 0; i < S1NPC.Inventory.SlotCount; i++)
                    {
                        var slot = new S1Items.ItemSlot();
#if MONOMELON
                        slot.SetSlotOwner(S1NPC.Inventory);
#else
                        slot.SetSlotOwner(S1NPC.Inventory.Cast<S1Items.IItemSlotOwner>());
#endif
#if (IL2CPPMELON || IL2CPPBEPINEX)
                        System.Action handler = new System.Action(() =>
                        {
                    if (S1NPC.Inventory != null && S1NPC.Inventory.onContentsChanged != null)
                        S1NPC.Inventory.onContentsChanged.Invoke();
                        });
                        slot.onItemDataChanged = (Il2CppSystem.Action)Il2CppSystem.Delegate.Combine(
                            slot.onItemDataChanged,
                            (Il2CppSystem.Action)handler
                        );
#else
                        slot.onItemDataChanged = (Action)Delegate.Combine(
                            slot.onItemDataChanged,
                    new Action(() =>
                    {
                        if (S1NPC.Inventory != null && S1NPC.Inventory.onContentsChanged != null)
                            S1NPC.Inventory.onContentsChanged.Invoke();
                    })
                        );
#endif
                        S1NPC.Inventory.ItemSlots.Add(slot);
                    }
                }
            }
            catch { /* ignore */ }

            if (S1NPC.Inventory.PickpocketIntObj == null)
            {
                S1Interaction.InteractableObject? talkInteractable = GetPrimaryInteractable();
                S1Interaction.InteractableObject[] interactables = gameObject.GetComponentsInChildren<S1Interaction.InteractableObject>(true);
                S1Interaction.InteractableObject? pickpocket = interactables.FirstOrDefault(io => io != null && io != talkInteractable);
                if (pickpocket == null)
                    pickpocket = gameObject.AddComponent<S1Interaction.InteractableObject>();

                S1NPC.Inventory.PickpocketIntObj = pickpocket;
            }
        }

        private void InitializeRelationshipData()
        {
            if (S1NPC.RelationData == null)
                S1NPC.RelationData = new S1Relation.NPCRelationData();

            // Ensure the relation data is bound to this NPC and initialized
            try
            {
                if (S1NPC.RelationData != null)
                {
                    S1NPC.RelationData.Init(S1NPC);
                }
            }
            catch { /* ignore: base game will handle in its own lifecycle if not ready */ }
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
                    Debug.LogWarning($"[S1API] Failed to initialize network behaviour {behaviour.GetType().Name}: {ex.Message}");
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
        private NPCRelationship _relationship;
        private bool _wasLoadedFromSave;

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
                Debug.LogWarning($"[S1API] Failed to queue NPC network spawn: {ex.Message}");
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
                Debug.LogWarning($"[S1API] Failed to prepare customer data before spawn: {ex.Message}");
            }
        }

        internal void FinalizeNetworkSpawn()
        {
            try
            {
                bool broadcastVisibility = InstanceFinder.IsServer;
                S1NPC.SetVisible(IsPhysical, networked: broadcastVisibility);

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
                    Debug.LogWarning($"[S1API] Failed to ensure Customer on NPC: {ex.Message}");
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
                                spec.ApplyTo(Schedule);
                        }
                        Schedule.InitializeActions();
                        Schedule.EnforceState();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[S1API] Failed to apply planned schedule: {ex.Message}");
                }

                // Apply per-type relationship defaults after base fields are present, unless loaded from save
                if (!_wasLoadedFromSave)
                {
                    try
                    {
                        var t = GetType();
                        if (TypeToRelationshipDefaults.TryGetValue(t, out var relCfg) && relCfg != null)
                        {
                            var builder = new NPCRelationshipDataBuilder();
                            relCfg(builder);
                            var rel = S1NPC.RelationData;
                            if (rel != null)
                                builder.ApplyTo(rel, S1NPC);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[S1API] Failed to apply relationship defaults: {ex.Message}");
                    }
                }

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
                    Debug.LogWarning($"[S1API] Failed to apply spawn position: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[S1API] Failed to finalize NPC after spawn: {ex.Message}");
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
