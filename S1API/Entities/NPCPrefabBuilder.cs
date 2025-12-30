#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1NPCsBehaviour = Il2CppScheduleOne.NPCs.Behaviour;
using S1Economy = Il2CppScheduleOne.Economy;
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1Items = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1NPCsBehaviour = ScheduleOne.NPCs.Behaviour;
using S1Economy = ScheduleOne.Economy;
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1Items = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
#endif

using System;
using System.Reflection;
using UnityEngine;
using S1API.Entities.Schedule;
using S1API.Entities.Customer;
using S1API.Entities.Dealer;
using S1API.Entities.Relation;
using System.Collections.Generic;
using S1API.Internal.Entities;
using S1API.Internal.Utils;
using S1API.Logging;

namespace S1API.Entities
{
    /// <summary>
    /// Builder for composing NPC prefab configuration before network spawn. Use to declare networked components,
    /// spawn position, customer behavior, relationships, schedules, and appearance defaults.
    /// </summary>
    /// <remarks>
    /// Configuration must be done in <see cref="NPC.ConfigurePrefab"/> for proper save/load behavior.
    /// All builder methods return the builder instance for fluent chaining.
    /// </remarks>
    public sealed class NPCPrefabBuilder
    {
        private static readonly Log Logger = new Log("NPCPrefabBuilder");
        private readonly GameObject prefabRoot;
        private readonly Type ownerType;

        internal NPCPrefabBuilder(GameObject prefabRoot, Type ownerType)
        {
            this.prefabRoot = prefabRoot;
            this.ownerType = ownerType;
        }

        /// <summary>
        /// Ensures an <see cref="S1NPCs.NPCScheduleManager"/> exists under the prefab root.
        /// Returns the manager instance for further configuration.
        /// </summary>
        private S1NPCs.NPCScheduleManager EnsureScheduleManager()
        {
            var mgr = prefabRoot.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
            if (mgr == null)
            {
                var go = new GameObject("NPCSchedule");
                go.transform.SetParent(prefabRoot.transform, false);
                mgr = go.AddComponent<S1NPCs.NPCScheduleManager>();
            }
            return mgr;
        }

        /// <summary>
        /// Adds customer behavior component to the NPC. Required before configuring customer defaults.
        /// </summary>
        /// <remarks>
        /// Enables the NPC to act as a business customer that can buy products from the player.
        /// </remarks>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder EnsureCustomer()
        {
            var customer = prefabRoot.GetComponent<S1Economy.Customer>();
            if (customer == null)
            {
                customer = prefabRoot.AddComponent<S1Economy.Customer>();
                customer.enabled = true;
            }
            // Mark this NPC type as a Customer-bearing type so pre-registration adds Customer on template
            NPC.RegisterCustomerType(ownerType);
            return this;
        }

        /// <summary>
        /// Declares identity defaults (ID, first and last name) to be embedded on the prefab.
        /// These values are applied on spawn on both server and clients.
        /// </summary>
        public NPCPrefabBuilder WithIdentity(string id, string firstName, string lastName)
        {
            try
            {
                var identity = EnsureIdentityComponent();
                identity.Id = id;
                identity.FirstName = firstName;
                identity.LastName = lastName;
                // Register to static cache for Il2Cpp network spawn support
                identity.RegisterToStaticCache(prefabRoot.name);
            }
            catch { }
            return this;
        }

        /// <summary>
        /// Declares the icon sprite to be embedded on the prefab.
        /// This sprite is used for UI elements such as messages, contacts, and relationships.
        /// Should be 64x64 or 128x128 pixels. Uses default if not set.
        /// </summary>
        /// <param name="icon">Optional sprite for UI elements. Uses default if null.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder WithIcon(Sprite? icon)
        {
            try
            {
                var identity = EnsureIdentityComponent();
                identity.Icon = icon;
                // Register to static cache for Il2Cpp network spawn support
                identity.RegisterToStaticCache(prefabRoot.name);
            }
            catch { }
            return this;
        }

        /// <summary>
        /// Declares appearance defaults via a wrapper builder. Values are embedded as an AvatarSettings
        /// asset reference on the prefab and applied to the runtime avatar on spawn (server and clients).
        /// </summary>
        public NPCPrefabBuilder WithAppearanceDefaults(Action<AvatarDefaultsBuilder> configure)
        {
            if (configure == null)
                return this;

            try
            {
                var builder = new AvatarDefaultsBuilder();
                configure(builder);

                // Create a new AvatarSettings ScriptableObject and populate from wrapper values
                var settings = ScriptableObject.CreateInstance<S1AvatarFramework.AvatarSettings>();
                settings.hideFlags = HideFlags.DontUnloadUnusedAsset;

                settings.Gender = builder.Gender;
                settings.Height = builder.Height;
                settings.Weight = builder.Weight;
                settings.SkinColor = builder.SkinColor;
                settings.LeftEyeLidColor = builder.LeftEyeLidColor;
                settings.RightEyeLidColor = builder.RightEyeLidColor;
                settings.EyeBallTint = builder.EyeBallTint;
                settings.EyeballMaterialIdentifier = builder.EyeballMaterialIdentifier ?? string.Empty;
                settings.PupilDilation = builder.PupilDilation;
                settings.EyebrowScale = builder.EyebrowScale;
                settings.EyebrowThickness = builder.EyebrowThickness;
                settings.EyebrowRestingHeight = builder.EyebrowRestingHeight;
                settings.EyebrowRestingAngle = builder.EyebrowRestingAngle;
                settings.HairPath = builder.HairPath ?? string.Empty;
                settings.HairColor = builder.HairColor;
                settings.LeftEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
                {
                    topLidOpen = builder.LeftEye.topLidOpen,
                    bottomLidOpen = builder.LeftEye.bottomLidOpen
                };
                settings.RightEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
                {
                    topLidOpen = builder.RightEye.topLidOpen,
                    bottomLidOpen = builder.RightEye.bottomLidOpen
                };

                // Layers
                var faceList = new List<S1AvatarFramework.AvatarSettings.LayerSetting>();
                var bodyList = new List<S1AvatarFramework.AvatarSettings.LayerSetting>();
                var accessoryList = new List<S1AvatarFramework.AvatarSettings.AccessorySetting>();
                if (settings.FaceLayerSettings == null)
                    settings.FaceLayerSettings = ToIl2CppList(faceList);
                if (settings.BodyLayerSettings == null)
                    settings.BodyLayerSettings = ToIl2CppList(bodyList);
                if (settings.AccessorySettings == null)
                    settings.AccessorySettings = ToIl2CppList(accessoryList);

                for (int i = 0; i < builder.FaceLayers.Count; i++)
                {
                    var l = builder.FaceLayers[i];
                    settings.FaceLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
                    {
                        layerPath = l.path,
                        layerTint = l.color
                    });
                }
                for (int i = 0; i < builder.BodyLayers.Count; i++)
                {
                    var l = builder.BodyLayers[i];
                    settings.BodyLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
                    {
                        layerPath = l.path,
                        layerTint = l.color
                    });
                }
                for (int i = 0; i < builder.AccessoryLayers.Count; i++)
                {
                    var l = builder.AccessoryLayers[i];
                    settings.AccessorySettings.Add(new S1AvatarFramework.AvatarSettings.AccessorySetting
                    {
                        path = l.path,
                        color = l.color
                    });
                }

                // Attach to prefab identity so clients can load it on spawn
                var identity = EnsureIdentityComponent();
                identity.AppearanceDefaults = settings;
                // Register to static cache for Il2Cpp network spawn support
                identity.RegisterToStaticCache(prefabRoot.name);

                // Apply settings directly to Avatar component on prefab to prevent destruction issues
                ApplyAvatarSettingsToPrefab(settings);
            }
            catch { }

            return this;
        }

        /// <summary>
        /// Defines the NPC's schedule using the <see cref="PrefabScheduleBuilder"/>. Schedule actions are planned and pre-created on the prefab.
        /// </summary>
        /// <remarks>
        /// Use to configure movement patterns, building visits, and timed activities. The plan is applied at runtime to activate precreated actions.
        /// Schedule configuration must be done in <see cref="NPC.ConfigurePrefab"/> for proper save/load behavior.
        /// </remarks>
        /// <param name="configure">Action to configure schedule using the builder.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder WithSchedule(Action<PrefabScheduleBuilder> configure)
        {
            if (configure == null)
                return this;

            try
            {
                var planner = new PrefabScheduleBuilder();
                configure(planner);
                var specs = planner.Build();
                return WithSchedule(specs);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to build schedule for NPC type {ownerType?.Name ?? "Unknown"}: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
                return this;
            }
        }

        /// <summary>
        /// Declares a schedule using a prebuilt set of specs. Use when composing plans externally or sharing between NPC types.
        /// </summary>
        /// <param name="specs">Enumerable collection of schedule action specifications.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder WithSchedule(IEnumerable<IScheduleActionSpec> specs)
        {
            if (specs == null)
                return this;

            try
            {
                // Materialize once to avoid multiple enumeration and allow counting for precreation
                var list = specs as List<IScheduleActionSpec> ?? new List<IScheduleActionSpec>(specs);
                if (list.Count == 0)
                    return this;

                NPC.RegisterSchedulePlanForType(ownerType, list);
                // Pre-create actions based on the plan to keep FishNet indices stable
                PrecreateActionsForSpecs(list);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to register schedule plan for NPC type {ownerType?.Name ?? "Unknown"}: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
            }

            return this;
        }

        /// <summary>
        /// Declares a schedule using a params array of specs for convenience.
        /// </summary>
        /// <param name="specs">Array of schedule action specifications.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder WithSchedule(params IScheduleActionSpec[] specs)
        {
            if (specs == null || specs.Length == 0)
                return this;
            return WithSchedule((IEnumerable<IScheduleActionSpec>)specs);
        }

        /// <summary>
        /// Adds dealer behavior to the NPC. Required before configuring dealer defaults.
        /// </summary>
        /// <remarks>
        /// Enables the NPC to act as a dealer that sells products to assigned customers.
        /// Note: Since Dealer inherits from NPC in the base game, dealer functionality is applied
        /// through configuration rather than component addition. This marks the NPC type as dealer-capable.
        /// When the NPC spawns, <see cref="NPCDealer.EnsureDealer"/> will be called automatically to initialize
        /// dealer functionality and ensure the messaging app displays the correct Dealer category badge.
        /// </remarks>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder EnsureDealer()
        {
            // Since Dealer inherits from NPC (not a component), we can't add it as a component.
            // Instead, we mark this NPC type as dealer-capable and store configuration.
            NPC.RegisterDealerType(ownerType);
            
            // Ensure required schedule components exist
            var mgr = EnsureScheduleManager();

            // Ensure DealerAttendDealBehaviour exists for dealer contract fulfillment (v0.4.2f4+)
            var npcBehaviour = prefabRoot.GetComponentInChildren<S1NPCsBehaviour.NPCBehaviour>(true);
            if (npcBehaviour == null)
            {
                var behGo = new GameObject("NPCBehaviour");
                behGo.transform.SetParent(mgr.transform, false);
                npcBehaviour = behGo.AddComponent<S1NPCsBehaviour.NPCBehaviour>();
            }

            var attendDeal = prefabRoot.GetComponentInChildren<S1NPCsBehaviour.DealerAttendDealBehaviour>(true);
            if (attendDeal == null)
            {
                var go = new GameObject("DealerAttendDealBehaviour");
                go.transform.SetParent(npcBehaviour.transform, false);
                attendDeal = go.AddComponent<S1NPCsBehaviour.DealerAttendDealBehaviour>();
                go.SetActive(false);
            }

            // Ensure NPCEvent_StayInBuilding exists for home behavior
            var stayInBuilding = prefabRoot.GetComponentInChildren<S1NPCsSchedules.NPCEvent_StayInBuilding>(true);
            if (stayInBuilding == null)
            {
                var go = new GameObject("StayInBuilding");
                go.transform.SetParent(mgr.transform, false);
                stayInBuilding = go.AddComponent<S1NPCsSchedules.NPCEvent_StayInBuilding>();
                go.SetActive(false);
            }
            
            return this;
        }

        /// <summary>
        /// Configures customer behavior defaults using the <see cref="CustomerDataBuilder"/>. Requires <see cref="EnsureCustomer"/> to be called first.
        /// </summary>
        /// <remarks>
        /// Configure spending behavior, order frequency, customer standards, product preferences, and relationship requirements.
        /// This configuration is essential for proper save/load behavior and must be done in <see cref="NPC.ConfigurePrefab"/>.
        /// </remarks>
        /// <param name="configure">Action to configure customer defaults using the builder.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder WithCustomerDefaults(Action<CustomerDataBuilder> configure)
        {
            if (configure == null)
                return this;

            EnsureCustomer();
            var customer = prefabRoot.GetComponent<S1Economy.Customer>();
            if (customer != null)
            {
                try
                {
                    var builder = new CustomerDataBuilder();
                    configure(builder);
                    var data = builder.BuildInternal();
#if MONOMELON
                    var field = typeof(S1Economy.Customer).GetField("customerData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    field?.SetValue(customer, data);
#else
                    customer.customerData = data;
#endif
                }
                catch { }
            }

            NPC.RegisterCustomerDefaultsForType(ownerType, configure);
            return this;
        }

        /// <summary>
        /// Configures default relationship settings (delta, unlock type, connections) for this NPC type.
        /// Applied to the instance after spawn and before save-data hydration.
        /// </summary>
        /// <remarks>
        /// Configure starting relationship level, unlock state, and connections to other NPCs.
        /// This configuration must be done in <see cref="NPC.ConfigurePrefab"/> for proper save/load behavior.
        /// </remarks>
        /// <param name="configure">Action to configure relationship defaults using the builder.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder WithRelationshipDefaults(Action<NPCRelationshipDataBuilder> configure)
        {
            if (configure != null)
            {
                NPC.RegisterRelationshipDefaultsForType(ownerType, configure);
                
                // Register relationship data to NPCPrefabIdentity for Il2Cpp compatibility
                try
                {
                    var builder = new NPCRelationshipDataBuilder();
                    configure(builder);
                    
                    NPCPrefabIdentity.RegisterRelationshipDataToStaticCache(prefabRoot.name, builder);
                }
                catch (Exception ex)
                {
                    Logger.Error($"[Relationship Data] WithRelationshipDefaults: Exception registering relationship data for prefab '{prefabRoot.name}': {ex.Message}");
                }
            }
            return this;
        }

        /// <summary>
        /// Sets the spawn position and rotation for this NPC type. Applied every time the NPC is spawned (new games and loaded games).
        /// </summary>
        /// <remarks>
        /// Use world coordinates. Consider building entrances, roads, and safe spawn areas. Position should be on a walkable surface.
        /// </remarks>
        /// <param name="position">World position where the NPC will spawn.</param>
        /// <param name="rotation">Rotation for the NPC (defaults to Quaternion.identity).</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder WithSpawnPosition(Vector3 position, Quaternion rotation)
        {
            NPC.RegisterSpawnPositionForType(ownerType, position, rotation);
            return this;
        }

        /// <summary>
        /// Sets the spawn position with default rotation. Applied every time the NPC is spawned.
        /// </summary>
        /// <param name="position">World position where the NPC will spawn.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder WithSpawnPosition(Vector3 position)
        {
            return WithSpawnPosition(position, Quaternion.identity);
        }

        /// <summary>
        /// Configures dealer behavior defaults using the <see cref="DealerDataBuilder"/>. Requires <see cref="EnsureDealer"/> to be called first.
        /// </summary>
        /// <remarks>
        /// Configure dealer settings such as signing fee, commission cut, dealer type, quality restrictions, and deal tracking.
        /// This configuration is essential for proper save/load behavior and must be done in <see cref="NPC.ConfigurePrefab"/>.
        /// </remarks>
        /// <param name="configure">Action to configure dealer defaults using the builder.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder WithDealerDefaults(Action<DealerDataBuilder> configure)
        {
            if (configure == null)
                return this;

            EnsureDealer();
            
            // Register dealer defaults for type-level application
            NPC.RegisterDealerDefaultsForType(ownerType, configure);
            
            // Note: Since Dealer inherits from NPC, we can't directly apply configuration to a component.
            // Configuration will be applied when the NPC instance is created as a Dealer.
            // This is handled in NPC.cs during FinalizeNetworkSpawn or similar lifecycle methods.
            
            return this;
        }

        /// <summary>
        /// Declares default inventory configuration for this NPC type. Supports startup items (always present) and random cash (varies on each sleep).
        /// </summary>
        /// <remarks>
        /// All configurations are optional. Applied when the NPC is spawned.
        /// </remarks>
        /// <param name="configure">Action to configure inventory defaults using the builder.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder WithInventoryDefaults(Action<RandomInventoryItemsBuilder> configure)
        {
            if (configure == null)
                return this;

            try
            {
                NPC.RegisterRandomInventoryDefaultsForType(ownerType, configure);
                
                // Optionally apply to prefab's NPCInventory component if it exists
                var inventory = prefabRoot.GetComponent<S1NPCs.NPCInventory>();
                if (inventory != null)
                {
                    var builder = new RandomInventoryItemsBuilder();
                    configure(builder);
                    var data = builder.BuildInternal();
                    ApplyInventoryDefaultsToComponent(inventory, data);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[S1API] Failed to register inventory defaults for {ownerType.Name}: {ex.Message}");
            }

            return this;
        }

        private void ApplyInventoryDefaultsToComponent(S1NPCs.NPCInventory inventory, RandomInventoryItemsBuilder.InventoryDefaultsData data)
        {
            if (inventory == null || data == null)
                return;

            try
            {
                // Apply random cash
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

                // Do NOT set StartupItems here on the prefab - they will be set during runtime initialization
                // in NPC.InitializeInventoryComponent to avoid duplicate insertion when NPCInventory.Awake runs.
                // StartupItems are processed by NPCInventory.Awake, and setting them on the prefab causes
                // items to be inserted when the prefab is instantiated, then again when ApplyRandomInventoryDefaults
                // sets them during initialization.
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[S1API] Failed to apply inventory defaults to prefab: {ex.Message}");
            }
        }

        private void PrecreateActionsForSpecs(List<IScheduleActionSpec> specs)
        {
            if (specs == null || specs.Count == 0)
                return;

            var mgr = EnsureScheduleManager();

            int walkTo = 0, stayInBuilding = 0, locationDialogue = 0, useVending = 0, driveToCarPark = 0, dealSignal = 0, useATM = 0;
            for (int i = 0; i < specs.Count; i++)
            {
                var s = specs[i];
                if (s is WalkToSpec) walkTo++;
                else if (s is StayInBuildingSpec) stayInBuilding++;
                else if (s is LocationDialogueSpec) locationDialogue++;
                else if (s is UseVendingMachineSpec) useVending++;
                else if (s is DriveToCarParkSpec) driveToCarPark++;
                else if (s is EnsureDealSignalSpec) dealSignal = Math.Max(dealSignal, 1);
                else if (s is UseATMSpec) useATM++;
            }

            if (dealSignal > 0)
            {
                EnsurePrefabAction<S1NPCsSchedules.NPCSignal_WaitForDelivery>(count: 1, namePrefix: "DealSignal");

                // Wire the deal signal to the Customer component so runtime deal handling works without relying on OnValidate
                try
                {
                    var scheduleManager = EnsureScheduleManager();
                    var signal = scheduleManager.GetComponentInChildren<S1NPCsSchedules.NPCSignal_WaitForDelivery>(true);
                    var customer = prefabRoot.GetComponent<S1Economy.Customer>();
                    if (signal != null && customer != null)
                    {
                        customer.DealSignal = signal;
                    }
                }
                catch (Exception ex)
                {
                    var npcTypeName = ownerType?.Name ?? "Unknown";
                    Logger.Warning($"Failed to wire DealSignal on prefab for NPC type {npcTypeName}: {ex.Message}");
                }
            }
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_WalkToLocation>(walkTo, "WalkTo");
            EnsurePrefabAction<S1NPCsSchedules.NPCEvent_StayInBuilding>(stayInBuilding, "StayInBuilding");
            EnsurePrefabAction<S1NPCsSchedules.NPCEvent_LocationDialogue>(locationDialogue, "LocationDialogue");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_UseVendingMachine>(useVending, "UseVending");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_DriveToCarPark>(driveToCarPark, "DriveToCarPark");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_UseATM>(useATM, "UseATM");
        }

        private void EnsurePrefabAction<T>(int count, string namePrefix) where T : S1NPCsSchedules.NPCAction
        {
            if (count <= 0)
                return;
            var mgr = EnsureScheduleManager();
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject(string.IsNullOrEmpty(namePrefix) ? typeof(T).Name : ($"{namePrefix}_{i + 1}"));
                go.transform.SetParent(mgr.transform, false);
                var comp = go.AddComponent<T>();
                try
                {
                    var baseNpc = prefabRoot.GetComponent<S1NPCs.NPC>();
                    var npcField = typeof(T).GetField("npc", BindingFlags.NonPublic | BindingFlags.Instance);
                    npcField?.SetValue(comp, baseNpc);
                    var schedField = typeof(T).GetField("schedule", BindingFlags.NonPublic | BindingFlags.Instance);
                    schedField?.SetValue(comp, mgr);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to set npc/schedule fields on prefab action {typeof(T).Name} for NPC type {ownerType?.Name ?? "Unknown"}: {ex.Message}");
                }
                go.SetActive(false);
                comp.enabled = false;
            }
        }

        private NPCPrefabIdentity EnsureIdentityComponent()
        {
            var identity = prefabRoot.GetComponent<NPCPrefabIdentity>();
            if (identity == null)
                identity = prefabRoot.AddComponent<NPCPrefabIdentity>();
            return identity;
        }

        /// <summary>
        /// Applies AvatarSettings directly to the Avatar component on the prefab to prevent destruction issues.
        /// Tries InitialAvatarSettings, SettingsToLoad, and CurrentSettings in that order.
        /// Uses ReflectionUtils to handle both field and property access across Mono/Il2Cpp boundaries.
        /// </summary>
        private void ApplyAvatarSettingsToPrefab(S1AvatarFramework.AvatarSettings settings)
        {
            if (settings == null)
                return;

            try
            {
                // Find Avatar component on prefab or its children
                var avatar = prefabRoot.GetComponent<S1AvatarFramework.Avatar>() 
                    ?? prefabRoot.GetComponentInChildren<S1AvatarFramework.Avatar>(true);
                
                if (avatar == null)
                    return;

                // Set hideFlags to prevent destruction
                settings.hideFlags = HideFlags.DontUnloadUnusedAsset;

                // Try to set InitialAvatarSettings first (prefab-level setting)
                if (ReflectionUtils.TrySetFieldOrProperty(avatar, "InitialAvatarSettings", settings))
                    return;

                // Try SettingsToLoad as fallback
                if (ReflectionUtils.TrySetFieldOrProperty(avatar, "SettingsToLoad", settings))
                    return;

                // Try CurrentSettings as last resort
                ReflectionUtils.TrySetFieldOrProperty(avatar, "CurrentSettings", settings);
            }
            catch { }
        }

#if (IL2CPPMELON)
        private static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(System.Collections.Generic.List<T> source)
        {
            var list = new Il2CppSystem.Collections.Generic.List<T>();
            if (source == null)
                return list;
            for (int i = 0; i < source.Count; i++)
                list.Add(source[i]);
            return list;
        }
#else
        private static System.Collections.Generic.List<T> ToIl2CppList<T>(System.Collections.Generic.List<T> source)
        {
            return source;
        }
#endif

        /// <summary>
        /// Wrapper for authoring appearance defaults without exposing game types to modders.
        /// </summary>
        public sealed class AvatarDefaultsBuilder
        {
            public float Gender { get; set; } = 0.0f;
            public float Height { get; set; } = 1.0f;
            public float Weight { get; set; } = 0.5f;
            public Color32 SkinColor { get; set; } = new Color32(150, 120, 95, 255);
            public Color LeftEyeLidColor { get; set; } = new Color32(150, 120, 95, 255);
            public Color RightEyeLidColor { get; set; } = new Color32(150, 120, 95, 255);
            public Color EyeBallTint { get; set; } = Color.white;
            public string EyeballMaterialIdentifier { get; set; } = "Default";
            public float PupilDilation { get; set; } = 1.0f;
            public float EyebrowScale { get; set; } = 1.0f;
            public float EyebrowThickness { get; set; } = 1.0f;
            public float EyebrowRestingHeight { get; set; } = 0.0f;
            public float EyebrowRestingAngle { get; set; } = 0.0f;
            public (float topLidOpen, float bottomLidOpen) LeftEye { get; set; } = (0.5f, 0.5f);
            public (float topLidOpen, float bottomLidOpen) RightEye { get; set; } = (0.5f, 0.5f);
            public string HairPath { get; set; } = string.Empty;
            public Color HairColor { get; set; } = Color.black;

            internal readonly List<(string path, Color color)> FaceLayers = new List<(string, Color)>();
            internal readonly List<(string path, Color color)> BodyLayers = new List<(string, Color)>();
            internal readonly List<(string path, Color color)> AccessoryLayers = new List<(string, Color)>();

            public AvatarDefaultsBuilder WithFaceLayer(string path, Color color)
            {
                if (!string.IsNullOrEmpty(path))
                    FaceLayers.Add((path, color));
                return this;
            }

            public AvatarDefaultsBuilder WithBodyLayer(string path, Color color)
            {
                if (!string.IsNullOrEmpty(path))
                    BodyLayers.Add((path, color));
                return this;
            }

            public AvatarDefaultsBuilder WithAccessoryLayer(string path, Color color)
            {
                if (!string.IsNullOrEmpty(path))
                    AccessoryLayers.Add((path, color));
                return this;
            }
        }
    }
}
