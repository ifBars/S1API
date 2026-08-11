#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1NPCsBehaviour = Il2CppScheduleOne.NPCs.Behaviour;
using S1NPCsOther = Il2CppScheduleOne.NPCs.Other;
using S1Economy = Il2CppScheduleOne.Economy;
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1Items = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
using S1CoreEquipping = Il2CppScheduleOne.Core.Equipping.Framework;
using S1MapBase = Il2CppScheduleOne.Map;
#elif MONOMELON
using S1NPCs = ScheduleOne.NPCs;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1NPCsBehaviour = ScheduleOne.NPCs.Behaviour;
using S1NPCsOther = ScheduleOne.NPCs.Other;
using S1Economy = ScheduleOne.Economy;
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1Items = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
using S1CoreEquipping = ScheduleOne.Core.Equipping.Framework;
using S1MapBase = ScheduleOne.Map;
#endif

using System;
using S1API.Entities.Equippables;
using System.Reflection;
using UnityEngine;
using S1API.Entities.Schedule;
using S1API.Entities.Customer;
using S1API.Entities.Dealer;
using S1API.Entities.Supplier;
using S1API.Entities.Impostors;
using S1API.Entities.Voices;
using S1API.Entities.Relation;
using S1API.Entities.Appearances.Base;
using System.Collections.Generic;
using System.Linq;
using S1API.Internal.Entities;
using S1API.Internal.Utils;
using S1API.Logging;
using S1API.Map;
using Object = UnityEngine.Object;

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

        internal const int DealerAttendDealPriority = 5;
        internal const string DealerHomeEventName = "DealerHomeEvent";
        internal const bool BehaviourObjectsRemainActive = true;

        internal static bool IsDealerHomeEventName(string? name) =>
            string.Equals(name, DealerHomeEventName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "HomeEvent", StringComparison.OrdinalIgnoreCase);

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
            if (mgr != null) return mgr;
            var go = new GameObject("NPCSchedule");
            go.transform.SetParent(prefabRoot.transform, false);
            mgr = go.AddComponent<S1NPCs.NPCScheduleManager>();
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
                NPCDataAccess.ApplyIdentity(
                    prefabRoot.GetComponent<S1NPCs.NPC>(),
                    id,
                    firstName,
                    lastName);
                // Register to static cache for Il2Cpp network spawn support
                identity.RegisterToStaticCache(prefabRoot.name);
            }
            catch
            {
                // ignored
            }

            return this;
        }

        /// <summary>
        /// Declares the icon sprite to be embedded on the prefab.
        /// This sprite is used for UI elements such as messages, contacts, and relationships.
        /// Should be 64x64 or 128x128 pixels. Uses default if not set.
        /// </summary>
        /// <param name="icon">Optional sprite for UI elements. Uses default if null.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder WithIcon(Sprite icon)
        {
            try
            {
                var identity = EnsureIdentityComponent();
                identity.Icon = icon;
                NPCDataAccess.ApplyIcon(prefabRoot.GetComponent<S1NPCs.NPC>(), icon);
                // Register to static cache for Il2Cpp network spawn support
                identity.RegisterToStaticCache(prefabRoot.name);
            }
            catch
            {
                // ignored
            }

            return this;
        }

        /// <summary>
        /// Declares appearance defaults via a wrapper builder. Values are embedded as an AvatarSettings
        /// asset reference on the prefab and applied to the runtime avatar on spawn (server and clients).
        /// </summary>
        public NPCPrefabBuilder WithAppearanceDefaults(Action<AvatarDefaultsBuilder> configure)
        {
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
                settings.UseCombinedLayer = false;
                settings.CombinedLayer = null;
                if ((builder.ImpostorSelection.Kind == AvatarImpostorSelectionKind.Texture ||
                     builder.ImpostorSelection.Kind == AvatarImpostorSelectionKind.Definition) &&
                    ImpostorTextureResolver.TryResolve(
                        builder.ImpostorSelection,
                        ownerType?.FullName ?? prefabRoot.name,
                        out Texture2D? impostorTexture))
                {
                    settings.ImpostorTexture = impostorTexture;
                }

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
                settings.FaceLayerSettings ??= ToIl2CppList(faceList);
                settings.BodyLayerSettings ??= ToIl2CppList(bodyList);
                settings.AccessorySettings ??= ToIl2CppList(accessoryList);

                foreach (var l in builder.FaceLayers)
                {
                    settings.FaceLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
                    {
                        layerPath = l.path,
                        layerTint = l.color
                    });
                }
                foreach (var l in builder.BodyLayers)
                {
                    settings.BodyLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
                    {
                        layerPath = l.path,
                        layerTint = l.color
                    });
                }
                foreach (var l in builder.AccessoryLayers)
                {
                    settings.AccessorySettings.Add(new S1AvatarFramework.AvatarSettings.AccessorySetting
                    {
                        path = l.path,
                        color = l.color
                    });
                }

                // Attach to prefab identity so clients can load it on spawn
                var identity = EnsureIdentityComponent();
                identity.AppearanceDefaults = settings;
                identity.AppearanceImpostorSelection = builder.ImpostorSelection;
                NPCDataAccess.ApplyAppearance(prefabRoot.GetComponent<S1NPCs.NPC>(), settings);
                // Register to static cache for Il2Cpp network spawn support
                identity.RegisterToStaticCache(prefabRoot.name);

                // Apply settings directly to Avatar component on prefab to prevent destruction issues
                ApplyAvatarSettingsToPrefab(settings);
            }
            catch (Exception ex)
            {
                string ownerName = ownerType?.FullName ?? "<unknown-NPC-type>";
                string assemblyName = ownerType?.Assembly.GetName().Name ?? "<unknown-assembly>";
                Logger.Warning(
                    $"[S1API][NPCAppearanceConfiguration] Failed to configure appearance defaults for " +
                    $"'{ownerName}' from assembly '{assemblyName}': {ex.GetType().Name}: {ex.Message}");
            }

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
            if (specs.Length == 0)
                return this;
            return WithSchedule((IEnumerable<IScheduleActionSpec>)specs);
        }

        /// <summary>
        /// Adds dealer behavior to the NPC. Required before configuring dealer defaults.
        /// </summary>
        /// <remarks>
        /// Enables the NPC to act as a dealer that sells products to assigned customers.
        /// This marks the NPC type as dealer-capable; S1API will ensure the generated spawnable prefab
        /// has a Dealer-compatible NPC component before network registration when the selected base prefab
        /// does not already include one.
        /// When the NPC spawns, <see cref="NPCDealer.EnsureDealer"/> will be called automatically to initialize
        /// dealer functionality and ensure the messaging app displays the correct Dealer category badge.
        /// </remarks>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder EnsureDealer()
        {
            // Mark the type as dealer-capable; NPC prefab creation materializes the correct runtime component.
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
            }
            attendDeal.gameObject.SetActive(BehaviourObjectsRemainActive);
            var baseNpcForDealer = prefabRoot.GetComponent<S1NPCs.NPC>();
            SetBehaviourRefs(attendDeal, npcBehaviour, baseNpcForDealer);
            attendDeal.Name = "Attend deal";
            attendDeal.Priority = DealerAttendDealPriority;

            // Keep the dealer's HomeEvent separate from mod-defined schedule actions. Dealer.OnTick
            // toggles this object directly, so reusing an arbitrary StayInBuilding action makes the
            // schedule and contract behaviour fight over the same doorway.
            var stayInBuilding = prefabRoot
                .GetComponentsInChildren<S1NPCsSchedules.NPCEvent_StayInBuilding>(true)
                .FirstOrDefault(action => IsDealerHomeEventName(action?.gameObject?.name));
            if (stayInBuilding == null)
            {
                var go = new GameObject(DealerHomeEventName);
                go.transform.SetParent(mgr.transform, false);
                stayInBuilding = go.AddComponent<S1NPCsSchedules.NPCEvent_StayInBuilding>();
                go.SetActive(false);
            }

            ReflectionUtils.TrySetFieldOrProperty(stayInBuilding, "npc", baseNpcForDealer);
            ReflectionUtils.TrySetFieldOrProperty(stayInBuilding, "schedule", mgr);
            if (baseNpcForDealer != null
                && CrossType.Is(baseNpcForDealer, out S1Economy.Dealer dealer))
                dealer.HomeEvent = stayInBuilding;

            return this;
        }

        /// <summary>
        /// Selects a supported base-game voice while preserving the prefab's inherited pitch.
        /// </summary>
        /// <param name="voice">The S1API-owned voice definition.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="voice"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the voice database or custom NPC data is unavailable.</exception>
        public NPCPrefabBuilder WithVoice(NPCVoiceDefinition voice) =>
            WithVoiceInternal(voice, null);

        /// <summary>
        /// Selects a supported base-game voice and default pitch.
        /// </summary>
        /// <param name="voice">The S1API-owned voice definition.</param>
        /// <param name="pitch">The default playback pitch, from 0.1 through 4.0.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="voice"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pitch"/> is outside the supported range.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the voice database or custom NPC data is unavailable.</exception>
        public NPCPrefabBuilder WithVoice(NPCVoiceDefinition voice, float pitch) =>
            WithVoiceInternal(voice, pitch);

        /// <summary>
        /// Selects a supported base-game voice by its stable, case-insensitive identifier.
        /// </summary>
        /// <param name="identifier">An identifier from <see cref="NPCVoiceCatalog"/>.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="identifier"/> is missing or unsupported.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the voice database or custom NPC data is unavailable.</exception>
        public NPCPrefabBuilder WithVoice(string identifier) =>
            WithVoice(NPCVoiceCatalog.Get(identifier));

        /// <summary>
        /// Selects a supported base-game voice and default pitch by identifier.
        /// </summary>
        /// <param name="identifier">An identifier from <see cref="NPCVoiceCatalog"/>.</param>
        /// <param name="pitch">The default playback pitch, from 0.1 through 4.0.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="identifier"/> is missing or unsupported.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pitch"/> is outside the supported range.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the voice database or custom NPC data is unavailable.</exception>
        public NPCPrefabBuilder WithVoice(string identifier, float pitch) =>
            WithVoice(NPCVoiceCatalog.Get(identifier), pitch);

        private NPCPrefabBuilder WithVoiceInternal(NPCVoiceDefinition voice, float? pitch)
        {
            if (voice == null)
                throw new ArgumentNullException(nameof(voice));

            if (pitch.HasValue
                && (float.IsNaN(pitch.Value)
                    || float.IsInfinity(pitch.Value)
                    || pitch.Value < 0.1f
                    || pitch.Value > 4f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pitch),
                    pitch,
                    "NPC voice pitch must be between 0.1 and 4.0.");
            }

            var npc = prefabRoot.GetComponent<S1NPCs.NPC>()
                      ?? throw new InvalidOperationException("The custom NPC prefab has no NPC component.");
            var database = NPCVoiceResolver.Resolve(voice);
            if (!NPCDataAccess.ApplyVoice(npc, database, pitch))
            {
                throw new InvalidOperationException(
                    $"Could not apply S1API voice '{voice.Id}' because the custom NPC has no framework data.");
            }

            var identity = EnsureIdentityComponent();
            identity.VoiceId = voice.Id;
            identity.VoicePitch = pitch;
            identity.RegisterToStaticCache(prefabRoot.name);

            return this;
        }

        /// <summary>
        /// Configures this NPC type to use the native supplier root.
        /// </summary>
        /// <remarks>
        /// Supplier NPCs support dead-drop orders, supplier meetings, delivery unlocks, and debt tracking.
        /// S1API reserves a location-dialogue schedule action required by the native supplier lifecycle.
        /// A custom NPC cannot be both a dealer and a supplier.
        /// </remarks>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder EnsureSupplier()
        {
            NPC.RegisterSupplierType(ownerType);
            SupplierRuntimeCoordinator.EnsurePrefabInfrastructure(prefabRoot);
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
                catch
                {
                    // ignored
                }
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
            EnsureDealer();
            
            // Register dealer defaults for type-level application
            NPC.RegisterDealerDefaultsForType(ownerType, configure);
            
            // Note: Since Dealer inherits from NPC, we can't directly apply configuration to a component.
            // Configuration will be applied when the NPC instance is created as a Dealer.
            // This is handled in NPC.cs during FinalizeNetworkSpawn or similar lifecycle methods.
            
            return this;
        }

        /// <summary>
        /// Sets the NPC's designated map region on the native prefab before it is network-spawned.
        /// </summary>
        /// <remarks>
        /// Configure this in <see cref="NPC.ConfigurePrefab"/> when the NPC has relationship connections.
        /// The native relationship initializer removes connections to NPCs in a different region, so setting
        /// <see cref="NPC.Region"/> later in <see cref="NPC.OnCreated"/> is too late.
        /// </remarks>
        /// <param name="region">The NPC's designated map region.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder WithRegion(Region region)
        {
            try
            {
                var npc = prefabRoot.GetComponent<S1NPCs.NPC>();
                if (npc != null)
                    npc.Region = (S1MapBase.EMapRegion)(int)region;
            }
            catch
            {
                // Keep the existing prefab defaults when the native NPC component is unavailable.
            }

            return this;
        }

        /// <summary>
        /// Configures native supplier data for this NPC type.
        /// </summary>
        /// <param name="configure">Action that defines order limits, delivery items, and supplier messages.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
        public NPCPrefabBuilder WithSupplierDefaults(Action<SupplierDataBuilder> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            EnsureSupplier();
            NPC.RegisterSupplierDefaultsForType(ownerType, configure);
            return this;
        }

        /// <summary>
        /// Adds smoke break behaviour to the NPC. Enables scheduled smoking with cigarette visual and animation.
        /// </summary>
        /// <remarks>
        /// Adds SmokeBreakBehaviour and SmokeCigarette. Requires cigarette equippable data. If cigarettePrefabPath is null,
        /// tries the current base game Resources path "equippables/cigarette/Cigarette", then runtime SmokeCigarette
        /// data fallbacks, then the legacy CigarettePrefab fallback for older game builds. Modders can pass a Resources
        /// path to an EquippableData asset or a TPEquippedItem prefab. Adds a placeholder smoke location.
        /// </remarks>
        /// <param name="cigarettePrefabPath">Resources path to cigarette EquippableData or TPEquippedItem. Null to try default paths.</param>
        /// <param name="debugMode">Whether to enable SmokeBreakBehaviour debug logging. Null leaves the current value unchanged.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder EnsureSmokeBreak(string? cigarettePrefabPath = null, bool? debugMode = null)
        {
            try
            {
                var cigaretteData = ResolveSmokeCigaretteData(cigarettePrefabPath);
                var legacyCigarettePrefab = cigaretteData == null
                    ? ResolveLegacySmokeCigarettePrefab(cigarettePrefabPath)
                    : null;

                if (cigaretteData == null && legacyCigarettePrefab == null)
                {
                    Logger.Warning("EnsureSmokeBreak: Could not load cigarette equippable data or legacy prefab. Pass a Resources path to an EquippableData asset or TPEquippedItem prefab in your mod (for example EnsureSmokeBreak(\"equippables/cigarette/Cigarette\")).");
                }

                var mgr = EnsureScheduleManager();
                var npcBehaviour = prefabRoot.GetComponentInChildren<S1NPCsBehaviour.NPCBehaviour>(true);
                if (npcBehaviour == null)
                {
                    var behGo = new GameObject("NPCBehaviour");
                    behGo.transform.SetParent(mgr.transform, false);
                    npcBehaviour = behGo.AddComponent<S1NPCsBehaviour.NPCBehaviour>();
                }

                var smokeBreak = npcBehaviour.GetComponentInChildren<S1NPCsBehaviour.SmokeBreakBehaviour>(true);
                if (smokeBreak == null)
                {
                    var go = new GameObject("SmokeBreakBehaviour");
                    go.transform.SetParent(npcBehaviour.gameObject.transform, false);
                    smokeBreak = go.AddComponent<S1NPCsBehaviour.SmokeBreakBehaviour>();
                }
                smokeBreak.gameObject.SetActive(BehaviourObjectsRemainActive);
                smokeBreak.Name = "SmokeBreakBehaviour";

                var smokeCigarette = smokeBreak.GetComponentInChildren<S1NPCsOther.SmokeCigarette>(true);
                if (smokeCigarette == null)
                {
                    var scGo = new GameObject("SmokeCigarette");
                    scGo.transform.SetParent(smokeBreak.transform, false);
                    smokeCigarette = scGo.AddComponent<S1NPCsOther.SmokeCigarette>();
                }

                var baseNpc = prefabRoot.GetComponent<S1NPCs.NPC>();
                var anim = prefabRoot.GetComponentInChildren<S1AvatarFramework.Animation.AvatarAnimation>(true);

                ReflectionUtils.TrySetFieldOrProperty(smokeCigarette, "Npc", baseNpc);
                ReflectionUtils.TrySetFieldOrProperty(smokeCigarette, "_npc", baseNpc);
                if (cigaretteData != null)
                    ReflectionUtils.TrySetFieldOrProperty(smokeCigarette, "_cigarette", cigaretteData);
                else if (legacyCigarettePrefab != null)
                    ReflectionUtils.TrySetFieldOrProperty(smokeCigarette, "CigarettePrefab", legacyCigarettePrefab);
                ReflectionUtils.TrySetFieldOrProperty(smokeCigarette, "Anim", anim);
                ReflectionUtils.TrySetFieldOrProperty(smokeBreak, "SmokeCigarette", smokeCigarette);
                if (debugMode.HasValue)
                {
                    ReflectionUtils.TrySetFieldOrProperty(smokeBreak, "_debugMode", debugMode.Value);
                }

                smokeBreak.MinMaxSmokeBreak = new Vector2Int(1, 2);
                if (smokeBreak.SmokeBreakLocations == null)
                {
#if (IL2CPPMELON)
                    smokeBreak.SmokeBreakLocations = new Il2CppSystem.Collections.Generic.List<Transform>();
#else
                    smokeBreak.SmokeBreakLocations = new List<Transform>();
#endif
                }
                if (smokeBreak.SmokeBreakLocations.Count == 0)
                {
                    var locGo = new GameObject("SmokeLocation_Default");
                    locGo.transform.SetParent(smokeBreak.transform, false);
                    var lookAt = new GameObject("LookAtPoint");
                    lookAt.transform.SetParent(locGo.transform, false);
                    lookAt.transform.localPosition = Vector3.forward;
                    smokeBreak.SmokeBreakLocations.Add(locGo.transform);
                }
                SetBehaviourRefs(smokeBreak, npcBehaviour, baseNpc);
            }
            catch (Exception ex)
            {
                Logger.Error($"EnsureSmokeBreak failed for {ownerType?.Name ?? "Unknown"}: {ex.Message}");
            }
            return this;
        }

        private static S1CoreEquipping.EquippableData? ResolveSmokeCigaretteData(string? resourcePath)
        {
            if (!string.IsNullOrWhiteSpace(resourcePath))
            {
                var explicitData = Resources.Load<S1CoreEquipping.EquippableData>(resourcePath);
                if (explicitData != null)
                    return explicitData;

                var explicitThirdPersonPrefab = Resources.Load<S1CoreEquipping.TPEquippedItem>(resourcePath);
                if (explicitThirdPersonPrefab != null)
                    return CreateSmokeCigaretteData(explicitThirdPersonPrefab);
            }

            var baseGameData = Resources.Load<S1CoreEquipping.EquippableData>("equippables/cigarette/Cigarette");
            if (baseGameData != null)
                return baseGameData;

            var baseGameThirdPersonPrefab = Resources.Load<S1CoreEquipping.TPEquippedItem>("equippables/cigarette/cigarette_tp");
            if (baseGameThirdPersonPrefab != null)
                return CreateSmokeCigaretteData(baseGameThirdPersonPrefab);

            try
            {
                var all = Object.FindObjectsOfType<S1NPCsOther.SmokeCigarette>(true);
                if (all != null)
                {
                    foreach (var smokeCigarette in all)
                    {
                        var data = ReflectionUtils.TryGetFieldOrProperty(smokeCigarette, "_cigarette")
                            as S1CoreEquipping.EquippableData;
                        if (data != null)
                            return data;
                    }
                }
            }
            catch
            {
                // Runtime fallback only; Resources paths above are the expected current path.
            }

            return null;
        }

        private static S1CoreEquipping.EquippableData CreateSmokeCigaretteData(
            S1CoreEquipping.TPEquippedItem thirdPersonPrefab)
        {
            var data = ScriptableObject.CreateInstance<S1CoreEquipping.EquippableData>();
            data.name = "S1API_Cigarette";
            data.ThirdPersonEquippedItemPrefab = thirdPersonPrefab;
            return data;
        }

        private static GameObject? ResolveLegacySmokeCigarettePrefab(string? resourcePath)
        {
            GameObject? cigarettePrefab = null;
            if (!string.IsNullOrWhiteSpace(resourcePath))
                cigarettePrefab = Resources.Load<GameObject>(resourcePath);

            if (cigarettePrefab == null)
                cigarettePrefab = Resources.Load<GameObject>("GameObject/Cigarette_Lit");

            if (cigarettePrefab != null)
                return cigarettePrefab;

            try
            {
                var all = Object.FindObjectsOfType<S1NPCsOther.SmokeCigarette>(true);
                if (all != null)
                {
                    foreach (var smokeCigarette in all)
                    {
                        var prefab = ReflectionUtils.TryGetFieldOrProperty(smokeCigarette, "CigarettePrefab") as GameObject;
                        if (prefab != null)
                            return prefab;
                    }
                }
            }
            catch
            {
                // Legacy fallback for pre-equippable game builds.
            }

            return null;
        }

        /// <summary>
        /// Adds graffiti behaviour to the NPC. Enables spray painting with spray can equip and effects.
        /// </summary>
        /// <remarks>
        /// Adds GraffitiBehaviour and SprayPaint. If sprayPaintEquippablePath is null, tries: (1) Resources
        /// "Weapons/SprayPaint/SprayPaint_AvatarEquippable", (2) runtime fallback: first SprayPaint in scene with
        /// non-null _sprayPaintPrefab. Modders can pass a Resources path or bundle a spray paint equippable.
        /// </remarks>
        /// <param name="sprayPaintEquippablePath">Resources path. Null for default. Use <see cref="EquippablePath.SprayPaint"/>.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder EnsureGraffiti(string? sprayPaintEquippablePath = null) =>
            EnsureGraffitiInternal(sprayPaintEquippablePath);

        /// <summary>Adds graffiti behaviour. Use <see cref="EquippablePath.SprayPaint"/> or <see cref="EquippablePath.Custom"/> for mod items.</summary>
        public NPCPrefabBuilder EnsureGraffiti(EquippablePath sprayPaintEquippablePath) =>
            EnsureGraffitiInternal(string.IsNullOrEmpty(sprayPaintEquippablePath.ResourcePath) ? null : sprayPaintEquippablePath.ResourcePath);

        private NPCPrefabBuilder EnsureGraffitiInternal(string? sprayPaintEquippablePath)
        {
            try
            {
                var path = sprayPaintEquippablePath ?? "Weapons/SprayPaint/SprayPaint_AvatarEquippable";
                var sprayPrefab = Resources.Load<GameObject>(path);
            S1AvatarFramework.Equipping.AvatarEquippable? sprayEquippable = null;
                if (sprayPrefab != null)
                    sprayEquippable = sprayPrefab.GetComponent<S1AvatarFramework.Equipping.AvatarEquippable>()
                        ?? sprayPrefab.GetComponentInChildren<S1AvatarFramework.Equipping.AvatarEquippable>(true);
                if (sprayEquippable == null && !string.IsNullOrEmpty(sprayPaintEquippablePath))
                {
                    sprayPrefab = Resources.Load<GameObject>(sprayPaintEquippablePath);
                    if (sprayPrefab != null)
                        sprayEquippable = sprayPrefab.GetComponent<S1AvatarFramework.Equipping.AvatarEquippable>()
                            ?? sprayPrefab.GetComponentInChildren<S1AvatarFramework.Equipping.AvatarEquippable>(true);
                }
                if (sprayEquippable == null)
                {
                    try
                    {
                        var all = Object.FindObjectsOfType<S1NPCsOther.SprayPaint>(true);
                        if (all != null)
                        {
                            foreach (var t in all)
                            {
                                var prefabObject = ReflectionUtils.TryGetFieldOrProperty(t, "_sprayPaintPrefab");
                                if (prefabObject == null
                                    || !CrossType.Is<S1AvatarFramework.Equipping.AvatarEquippable>(prefabObject, out var prefab)
                                    || prefab == null)
                                {
                                    continue;
                                }

                                sprayEquippable = prefab;
                                break;
                            }
                        }
                    }
                    catch { /* ignore */ }
                }

                var npcBehaviour = prefabRoot.GetComponentInChildren<S1NPCsBehaviour.NPCBehaviour>(true);
                if (npcBehaviour == null)
                {
                    var behGo = new GameObject("NPCBehaviour");
                    behGo.transform.SetParent(EnsureScheduleManager().transform, false);
                    npcBehaviour = behGo.AddComponent<S1NPCsBehaviour.NPCBehaviour>();
                }

                var graffiti = npcBehaviour.GetComponentInChildren<S1NPCsBehaviour.GraffitiBehaviour>(true);
                if (graffiti == null)
                {
                    var go = new GameObject("GraffitiBehaviour");
                    go.transform.SetParent(npcBehaviour.gameObject.transform, false);
                    graffiti = go.AddComponent<S1NPCsBehaviour.GraffitiBehaviour>();
                }
                graffiti.gameObject.SetActive(BehaviourObjectsRemainActive);
                graffiti.Name = "GraffitiBehaviour";

                var sprayPaint = graffiti.GetComponentInChildren<S1NPCsOther.SprayPaint>(true);
                if (sprayPaint == null)
                {
                    var spGo = new GameObject("SprayPaint");
                    spGo.transform.SetParent(graffiti.transform, false);
                    sprayPaint = spGo.AddComponent<S1NPCsOther.SprayPaint>();
                }

                if (sprayEquippable == null)
                {
                    var configuredPrefab = ReflectionUtils.TryGetFieldOrProperty(sprayPaint, "_sprayPaintPrefab");
                    if (configuredPrefab != null
                        && CrossType.Is<S1AvatarFramework.Equipping.AvatarEquippable>(configuredPrefab, out var inheritedPrefab)
                        && inheritedPrefab != null)
                    {
                        sprayEquippable = inheritedPrefab;
                    }
                }

                if (sprayEquippable == null)
                    Logger.Warning($"EnsureGraffiti: Could not load spray paint equippable at '{path}'. Spray painting may not work. Supply a valid path or register via AvatarEquippableRegistry.");

                var baseNpc = prefabRoot.GetComponent<S1NPCs.NPC>();

                ReflectionUtils.TrySetFieldOrProperty(graffiti, "_sprayPaint", sprayPaint);
                ReflectionUtils.TrySetFieldOrProperty(sprayPaint, "_npc", baseNpc);
                if (sprayEquippable != null)
                    ReflectionUtils.TrySetFieldOrProperty(sprayPaint, "_sprayPaintPrefab", sprayEquippable);
                SetBehaviourRefs(graffiti, npcBehaviour, baseNpc);
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.blue, 0.5f), new GradientColorKey(Color.green, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                ReflectionUtils.TrySetFieldOrProperty(graffiti, "_effectColorGradient", gradient);
            }
            catch (Exception ex)
            {
                Logger.Error($"EnsureGraffiti failed for {ownerType?.Name ?? "Unknown"}: {ex.Message}");
            }
            return this;
        }

        /// <summary>
        /// Adds drinking behaviour to the NPC. Enables equipping a drink and playing the drinking animation.
        /// </summary>
        /// <remarks>
        /// Adds DrinkItem as a standalone component. Use asset paths like "Avatar/Equippables/Beer" or "Avatar/Equippables/Coffee".
        /// </remarks>
        /// <param name="drinkEquippablePath">Resources path. Null for default. Use <see cref="EquippablePath.Beer"/>, <see cref="EquippablePath.Coffee"/>, etc.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder EnsureDrinking(string? drinkEquippablePath = null)
        {
            try
            {
                var path = drinkEquippablePath ?? "Avatar/Equippables/Beer";
                var drinkPrefab = Resources.Load<GameObject>(path);
                S1AvatarFramework.Equipping.AvatarEquippable? drinkEquippable = null;
                if (drinkPrefab != null)
                    drinkEquippable = drinkPrefab.GetComponent<S1AvatarFramework.Equipping.AvatarEquippable>()
                        ?? drinkPrefab.GetComponentInChildren<S1AvatarFramework.Equipping.AvatarEquippable>(true);
                if (drinkEquippable == null)
                    Logger.Warning($"EnsureDrinking: Could not load drink equippable at '{path}'.");

                var npcBehaviour = prefabRoot.GetComponentInChildren<S1NPCsBehaviour.NPCBehaviour>(true);
                if (npcBehaviour == null)
                {
                    var behGo = new GameObject("NPCBehaviour");
                    behGo.transform.SetParent(EnsureScheduleManager().transform, false);
                    npcBehaviour = behGo.AddComponent<S1NPCsBehaviour.NPCBehaviour>();
                }

                var drinkItem = npcBehaviour.GetComponentInChildren<S1NPCsOther.DrinkItem>(true);
                if (drinkItem == null)
                {
                    var go = new GameObject("DrinkItem");
                    go.transform.SetParent(npcBehaviour.transform, false);
                    drinkItem = go.AddComponent<S1NPCsOther.DrinkItem>();
                }

                var baseNpc = prefabRoot.GetComponent<S1NPCs.NPC>();
                ReflectionUtils.TrySetFieldOrProperty(drinkItem, "Npc", baseNpc);
                if (drinkEquippable != null)
                    ReflectionUtils.TrySetFieldOrProperty(drinkItem, "DrinkPrefab", drinkEquippable);
            }
            catch (Exception ex)
            {
                Logger.Error($"EnsureDrinking failed for {ownerType?.Name ?? "Unknown"}: {ex.Message}");
            }
            return this;
        }

        /// <summary>Adds drinking behaviour. Use <see cref="EquippablePath.Beer"/>, <see cref="EquippablePath.Coffee"/>, etc.</summary>
        public NPCPrefabBuilder EnsureDrinking(EquippablePath drinkEquippablePath) =>
            EnsureDrinking(string.IsNullOrEmpty(drinkEquippablePath.ResourcePath) ? null : drinkEquippablePath.ResourcePath);

        /// <summary>
        /// Adds generic item holding behaviour to the NPC. Enables equipping any AvatarEquippable item.
        /// </summary>
        /// <remarks>
        /// Adds HoldItem as a standalone component. Use <see cref="EquippablePath.Phone_Lowered"/>, <see cref="EquippablePath.Flashlight"/>, etc.
        /// </remarks>
        /// <param name="equippablePath">Resources path. Null for default. Use <see cref="EquippablePath"/> constants.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public NPCPrefabBuilder EnsureItemHolding(string? equippablePath = null)
        {
            try
            {
                var path = equippablePath ?? "Avatar/Equippables/Phone_Lowered";
                var equippablePrefab = Resources.Load<GameObject>(path);
                S1AvatarFramework.Equipping.AvatarEquippable? equippable = null;
                if (equippablePrefab != null)
                    equippable = equippablePrefab.GetComponent<S1AvatarFramework.Equipping.AvatarEquippable>()
                        ?? equippablePrefab.GetComponentInChildren<S1AvatarFramework.Equipping.AvatarEquippable>(true);
                if (equippable == null)
                    Logger.Warning($"EnsureItemHolding: Could not load equippable at '{path}'.");

                var npcBehaviour = prefabRoot.GetComponentInChildren<S1NPCsBehaviour.NPCBehaviour>(true);
                if (npcBehaviour == null)
                {
                    var behGo = new GameObject("NPCBehaviour");
                    behGo.transform.SetParent(EnsureScheduleManager().transform, false);
                    npcBehaviour = behGo.AddComponent<S1NPCsBehaviour.NPCBehaviour>();
                }

                var holdItem = npcBehaviour.GetComponentInChildren<S1NPCsOther.HoldItem>(true);
                if (holdItem == null)
                {
                    var go = new GameObject("HoldItem");
                    go.transform.SetParent(npcBehaviour.transform, false);
                    holdItem = go.AddComponent<S1NPCsOther.HoldItem>();
                }

                var baseNpc = prefabRoot.GetComponent<S1NPCs.NPC>();
                ReflectionUtils.TrySetFieldOrProperty(holdItem, "Npc", baseNpc);
                if (equippable != null) ReflectionUtils.TrySetFieldOrProperty(holdItem, "Equippable", equippable);
            }
            catch (Exception ex)
            {
                Logger.Error($"EnsureItemHolding failed for {ownerType?.Name ?? "Unknown"}: {ex.Message}");
            }
            return this;
        }

        /// <summary>Adds item holding behaviour. Use <see cref="EquippablePath.Phone_Lowered"/>, <see cref="EquippablePath.Flashlight"/>, etc.</summary>
        public NPCPrefabBuilder EnsureItemHolding(EquippablePath equippablePath) =>
            EnsureItemHolding(string.IsNullOrEmpty(equippablePath.ResourcePath) ? null : equippablePath.ResourcePath);

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
            if (inventory == null)
                return;

            try
            {
                // Apply random cash
                if (data.RandomCashMin.HasValue || data.RandomCashMax.HasValue)
                {
                    ReflectionUtils.TrySetFieldOrProperty(inventory, "RandomCash", true);
                    if (data.RandomCashMin.HasValue)
                        ReflectionUtils.TrySetFieldOrProperty(inventory, "RandomCashMin", data.RandomCashMin.Value);
                    if (data.RandomCashMax.HasValue)
                        ReflectionUtils.TrySetFieldOrProperty(inventory, "RandomCashMax", data.RandomCashMax.Value);
                }

                // Apply ClearInventoryEachNight setting
                if (data.ClearInventoryEachNight.HasValue)
                    ReflectionUtils.TrySetFieldOrProperty(inventory, "ClearInventoryEachNight", data.ClearInventoryEachNight.Value);

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
            if (specs.Count == 0)
                return;

            EnsureScheduleManager();

            int walkTo = 0, stayInBuilding = 0, locationDialogue = 0, locationBasedAction = 0, useVending = 0, driveToCarPark = 0, dealSignal = 0, useATM = 0, sit = 0;
            bool requiresSmokeBreak = false, requiresGraffiti = false, requiresDrinking = false, requiresHoldItem = false;
            foreach (var s in specs)
            {
                switch (s)
                {
                    case WalkToSpec:
                        walkTo++;
                        break;
                    case StayInBuildingSpec:
                        stayInBuilding++;
                        break;
                    case LocationDialogueSpec:
                        locationDialogue++;
                        break;
                    case LocationBasedActionSpec locationBasedSpec:
                        locationBasedAction++;
                        switch (locationBasedSpec.ArriveBehaviour)
                        {
                            case LocationArriveBehaviour.SmokeBreak:
                                requiresSmokeBreak = true;
                                break;
                            case LocationArriveBehaviour.Graffiti:
                                requiresGraffiti = true;
                                break;
                            case LocationArriveBehaviour.Drinking:
                                requiresDrinking = true;
                                break;
                            case LocationArriveBehaviour.HoldItem:
                                requiresHoldItem = true;
                                break;
                        }

                        break;
                    case UseVendingMachineSpec:
                        useVending++;
                        break;
                    case DriveToCarParkSpec:
                        driveToCarPark++;
                        break;
#pragma warning disable CS0618 // Compatibility spec retained for older compiled mods.
                    case EnsureDealSignalSpec:
                        dealSignal = Math.Max(dealSignal, 1);
                        break;
#pragma warning restore CS0618
                    case UseATMSpec:
                        useATM++;
                        break;
                    case SitSpec:
                        sit++;
                        break;
                }
            }

            if (requiresSmokeBreak)
                EnsureSmokeBreak();
            if (requiresGraffiti)
                EnsureGraffiti();
            if (requiresDrinking)
                EnsureDrinking();
            if (requiresHoldItem)
                EnsureItemHolding();

            if (dealSignal > 0)
            {
#if IL2CPPMELON
                EnsureIl2CppCustomerAttendDealBehaviour();
#else
                try
                {
                    NPCCustomer.EnsureDealAttendanceSupport(prefabRoot, ownerType);
                }
                catch (Exception ex)
                {
                    var npcTypeName = ownerType?.Name ?? "Unknown";
                    Logger.Warning($"Failed to configure customer deal attendance on prefab for NPC type {npcTypeName}: {ex.Message}");
                }
#endif
            }
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_WalkToLocation>(walkTo, "WalkTo");
            EnsurePrefabAction<S1NPCsSchedules.NPCEvent_StayInBuilding>(stayInBuilding, "StayInBuilding");
            EnsurePrefabAction<S1NPCsSchedules.NPCEvent_LocationDialogue>(locationDialogue, "LocationDialogue");
            EnsurePrefabAction<S1NPCsSchedules.NPCEvent_LocationBasedAction>(locationBasedAction, "LocationBasedAction");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_UseVendingMachine>(useVending, "UseVending");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_DriveToCarPark>(driveToCarPark, "DriveToCarPark");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_UseATM>(useATM, "UseATM");
            EnsurePrefabAction<S1NPCsSchedules.NPCEvent_Sit>(sit, "Sit");
        }

        private void EnsurePrefabAction<T>(int count, string namePrefix) where T : S1NPCsSchedules.NPCAction
        {
            if (count <= 0)
                return;
            var mgr = EnsureScheduleManager();
            for (var i = 0; i < count; i++)
            {
                var go = new GameObject(string.IsNullOrEmpty(namePrefix) ? typeof(T).Name : ($"{namePrefix}_{i + 1}"));
                go.transform.SetParent(mgr.transform, false);
                var comp = go.AddComponent<T>();
                try
                {
                    var baseNpc = prefabRoot.GetComponent<S1NPCs.NPC>();
                    Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(comp, "npc", baseNpc);
                    Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(comp, "schedule", mgr);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to set npc/schedule fields on prefab action {typeof(T).Name} for NPC type {ownerType?.Name ?? "Unknown"}: {ex.Message}");
                }
                go.SetActive(false);
                comp.enabled = false;
            }

        }

#if IL2CPPMELON
        private void EnsureIl2CppCustomerAttendDealBehaviour()
        {
            var behaviourManager = prefabRoot.GetComponentInChildren<S1NPCsBehaviour.NPCBehaviour>(true);
            if (behaviourManager == null)
            {
                Logger.Warning($"Cannot add CustomerAttendDealBehaviour for NPC type {ownerType?.Name ?? "Unknown"}: NPCBehaviour is missing.");
                return;
            }

            var component = prefabRoot.GetComponentInChildren<S1NPCsBehaviour.CustomerAttendDealBehaviour>(true);
            if (component == null)
            {
                var behaviourObject = new GameObject("Customer attend deal");
                behaviourObject.transform.SetParent(behaviourManager.transform, false);
                component = behaviourObject.AddComponent<S1NPCsBehaviour.CustomerAttendDealBehaviour>();
            }

            component.gameObject.SetActive(BehaviourObjectsRemainActive);
            ReflectionUtils.TrySetFieldOrProperty(component, "EnabledOnAwake", false);
            ReflectionUtils.TrySetFieldOrProperty(component, "Name", "Customer attend deal");
            ReflectionUtils.TrySetFieldOrProperty(component, "Priority", 4);
            component.SetCanUseUmbrellaDuringBehaviour(true);
        }
#endif

        /// <summary>
        /// Sets beh (NPCBehaviour) and ensures NPCBehaviour.Npc on Behaviour instances.
        /// Required because prefab build may run before Awake; Enable_Server uses beh.
        /// </summary>
        private void SetBehaviourRefs(S1NPCsBehaviour.Behaviour behaviour, S1NPCsBehaviour.NPCBehaviour npcBehaviour, S1NPCs.NPC baseNpc)
        {
            if (behaviour == null || npcBehaviour == null) return;
            ReflectionUtils.TrySetFieldOrProperty(behaviour, "beh", npcBehaviour);
            ReflectionUtils.TrySetFieldOrProperty(npcBehaviour, "Npc", baseNpc);
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
            catch
            {
                // ignored
            }
        }

#if (IL2CPPMELON)
        private static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(System.Collections.Generic.List<T> source)
        {
            var list = new Il2CppSystem.Collections.Generic.List<T>();
            foreach (var t in source)
                list.Add(t);

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
            internal AvatarImpostorSelection ImpostorSelection { get; private set; } = AvatarImpostorSelection.Preserve();

            public AvatarDefaultsBuilder WithFaceLayer(string path, Color color)
            {
                if (!string.IsNullOrEmpty(path))
                    FaceLayers.Add((path, color));
                return this;
            }

            public AvatarDefaultsBuilder WithFaceLayer<T>(string path, Color color)
                where T : BaseFaceAppearance =>
                WithFaceLayer(path, color);

            public AvatarDefaultsBuilder WithBodyLayer(string path, Color color)
            {
                if (!string.IsNullOrEmpty(path))
                    BodyLayers.Add((path, color));
                return this;
            }

            public AvatarDefaultsBuilder WithBodyLayer<T>(string path, Color color)
                where T : BaseBodyAppearance =>
                WithBodyLayer(path, color);

            public AvatarDefaultsBuilder WithAccessoryLayer(string path, Color color)
            {
                if (!string.IsNullOrEmpty(path))
                    AccessoryLayers.Add((path, color));
                return this;
            }

            public AvatarDefaultsBuilder WithAccessoryLayer<T>(string path, Color color)
                where T : BaseAccessoryAppearance =>
                WithAccessoryLayer(path, color);

            /// <summary>
            /// Uses an existing game-owned NPC impostor by character settings name.
            /// </summary>
            /// <param name="name">
            /// Name such as <c>Kyle</c>, or a resource path such as <c>charactersettings/Kyle</c>.
            /// </param>
            /// <returns>The current builder for chaining.</returns>
            public AvatarDefaultsBuilder WithImpostor(string name)
            {
                if (!string.IsNullOrWhiteSpace(name))
                    ImpostorSelection = AvatarImpostorSelection.FromName(name);
                return this;
            }

            /// <summary>
            /// Uses an existing game-owned NPC impostor discovered through <see cref="NPCImpostorCatalog"/>.
            /// </summary>
            /// <param name="impostor">The discovered impostor definition to use.</param>
            /// <returns>The current builder for chaining.</returns>
            public AvatarDefaultsBuilder WithImpostor(AvatarImpostorDefinition impostor)
            {
                if (impostor != null)
                    ImpostorSelection = AvatarImpostorSelection.FromDefinition(impostor);
                return this;
            }

            /// <summary>
            /// Uses a texture supplied by the mod as this NPC's far-distance impostor.
            /// </summary>
            /// <remarks>
            /// This is appropriate for textures loaded from an embedded resource with
            /// <c>TextureUtils.LoadTextureFromResource(...)</c>. S1API does not network raw texture bytes;
            /// every visual client must be able to load the same texture locally.
            /// </remarks>
            /// <param name="texture">Texture to use for the impostor.</param>
            /// <returns>The current builder for chaining.</returns>
            public AvatarDefaultsBuilder WithImpostorTexture(Texture2D texture)
            {
                if (texture != null)
                    ImpostorSelection = AvatarImpostorSelection.FromTexture(texture);
                return this;
            }

            /// <summary>
            /// Uses a deterministic random game-owned NPC impostor from all available impostors.
            /// </summary>
            /// <returns>The current builder for chaining.</returns>
            public AvatarDefaultsBuilder WithRandomImpostor()
            {
                ImpostorSelection = AvatarImpostorSelection.Random(null, null);
                return this;
            }

            /// <summary>
            /// Uses a deterministic random game-owned NPC impostor from the supplied character settings names.
            /// </summary>
            /// <param name="names">Names such as <c>Kyle</c>, <c>Mac</c>, or <c>Mick</c>.</param>
            /// <returns>The current builder for chaining.</returns>
            public AvatarDefaultsBuilder WithRandomImpostor(params string[] names)
            {
                ImpostorSelection = AvatarImpostorSelection.Random(CopyNames(names), null);
                return this;
            }

            /// <summary>
            /// Uses a seeded random game-owned NPC impostor from the supplied character settings names.
            /// </summary>
            /// <param name="seed">Seed used to select the impostor.</param>
            /// <param name="names">Names such as <c>Kyle</c>, <c>Mac</c>, or <c>Mick</c>.</param>
            /// <returns>The current builder for chaining.</returns>
            public AvatarDefaultsBuilder WithRandomImpostor(int seed, params string[] names)
            {
                ImpostorSelection = AvatarImpostorSelection.Random(CopyNames(names), seed);
                return this;
            }

            private static IReadOnlyList<string>? CopyNames(IEnumerable<string>? names)
            {
                if (names == null)
                    return null;

                var result = new List<string>();
                foreach (string name in names)
                {
                    if (!string.IsNullOrWhiteSpace(name))
                        result.Add(name);
                }

                return result.Count == 0 ? null : result;
            }
        }
    }
}
