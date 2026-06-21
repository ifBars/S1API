#if (IL2CPPMELON)
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1Economy = Il2CppScheduleOne.Economy;
using Il2CppInterop.Runtime.Attributes;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1NPCs = ScheduleOne.NPCs;
using S1Economy = ScheduleOne.Economy;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MelonLoader;
using S1API.Map;
using S1API.Internal.Map;
using S1API.Internal.Utils;
using S1API.Entities;
using S1API.Entities.Relation;
using S1API.Logging;

namespace S1API.Internal.Entities
{
    /// <summary>
    /// INTERNAL: Stores identity and appearance defaults on the prefab so clients receive
    /// the same configuration on network spawn without relying on RPCs/SyncVars.
    /// On Il2Cpp, stores data in a static registry keyed by prefab name to work around
    /// field serialization issues with RegisterTypeInIl2Cpp components.
    /// </summary>
#if IL2CPPMELON
    [RegisterTypeInIl2Cpp]
#endif
    internal sealed class NPCPrefabIdentity : MonoBehaviour
    {
        private static readonly Log Logger = new Log("NPCPrefabIdentity");

        // Mono prefab cloning relies on Unity serialization for these backing fields.
#if IL2CPPMELON
        private string? _id;
        private string? _firstName;
        private string? _lastName;
        private Sprite? _icon;
        private S1AvatarFramework.AvatarSettings? _appearanceDefaults;
        private AvatarImpostorSelection? _appearanceImpostorSelection;
        private string? _dealerHomeBuildingName;
        private string? _prefabName;
        private List<string>? _connectionIds;
#else
        [SerializeField] private string? _id;
        [SerializeField] private string? _firstName;
        [SerializeField] private string? _lastName;
        [SerializeField] private Sprite? _icon;
        [SerializeField] private S1AvatarFramework.AvatarSettings? _appearanceDefaults;
        private AvatarImpostorSelection? _appearanceImpostorSelection;
        [SerializeField] private string? _dealerHomeBuildingName;
        [SerializeField] private string? _prefabName;
        [SerializeField] private List<string>? _connectionIds;
#endif

        private float? _relationDelta;
        private bool? _unlocked;
        private NPCRelationship.UnlockType? _unlockType;

        // Static registry to preserve data across network instantiation on Il2Cpp
        private static readonly Dictionary<string, IdentityData> _registry = new Dictionary<string, IdentityData>();
        private bool _applied;
        private AvatarSettingsData _cachedAppearanceDefaults;

        internal string? Id
        {
            get => _id;
            set => _id = value;
        }

        internal string? FirstName
        {
            get => _firstName;
            set => _firstName = value;
        }

        internal string? LastName
        {
            get => _lastName;
            set => _lastName = value;
        }

        internal Sprite? Icon
        {
            get => _icon;
            set => _icon = value;
        }

        internal S1AvatarFramework.AvatarSettings? AppearanceDefaults
        {
            get => _appearanceDefaults;
            set => _appearanceDefaults = value;
        }

        internal AvatarImpostorSelection? AppearanceImpostorSelection
        {
#if IL2CPPMELON
            [HideFromIl2Cpp]
#endif
            get => _appearanceImpostorSelection;
#if IL2CPPMELON
            [HideFromIl2Cpp]
#endif
            set => _appearanceImpostorSelection = value;
        }

        internal string? DealerHomeBuildingName
        {
            get => _dealerHomeBuildingName;
            set => _dealerHomeBuildingName = value;
        }

        internal string? PrefabName
        {
            get => _prefabName;
            set => _prefabName = value;
        }

        private float? RelationDelta
        {
            get => _relationDelta;
            set => _relationDelta = value;
        }

        private bool? Unlocked
        {
            get => _unlocked;
            set => _unlocked = value;
        }

        private NPCRelationship.UnlockType? UnlockType
        {
            get => _unlockType;
            set => _unlockType = value;
        }

        private struct IdentityData
        {
            internal string Id;
            internal string FirstName;
            internal string LastName;
            internal Sprite Icon;
            internal AvatarSettingsData AppearanceDefaults;
            internal AvatarImpostorSelection AppearanceImpostorSelection;
            internal string DealerHomeBuildingName;
            internal float? RelationDelta;
            internal bool? Unlocked;
            internal int? UnlockType; // Stored as int (0=Recommendation, 1=DirectApproach) to avoid enum dependency
            internal List<string> ConnectionIDs;
            internal string PrefabName;
        }

        private void Awake()
        {
            // Try to restore from registry early, before GameObject name might be changed
            // This helps when the GameObject name changes from prefab name to first name
            TryRestoreFromRegistry();
        }

        private void Start()
        {
            // On Il2Cpp, restore fields from registry again in Start() in case they were wiped after Awake()
#if IL2CPPMELON
            TryRestoreFromRegistry();
#else
            // On Mono, also ensure dealer home building name is restored from registry
            EnsureDealerHomeBuildingNameFromRegistry();
#endif
            // apply immediately, then retry briefly in case Avatar isn't yet available on clients.
            TryApplyNow();
            if (!_applied)
                MelonCoroutines.Start(DelayedApply());
        }

        /// <summary>
        /// INTERNAL: Called by NPCPrefabBuilder to register relationship data for Il2Cpp.
        /// Extracts data from the builder and stores it in the static registry.
        /// </summary>
#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        internal static void RegisterRelationshipDataToStaticCache(string prefabName, NPCRelationshipDataBuilder builder)
        {
            if (string.IsNullOrEmpty(prefabName) || builder == null)
                return;

            try
            {
                // Normalize prefab name (remove "(Clone)" suffix) - only register prefabs, not spawned instances
                string normalizedName = prefabName;
                if (normalizedName.EndsWith("(Clone)"))
                    normalizedName = normalizedName.Substring(0, normalizedName.Length - 7);

                // Extract data from builder without reflection for Il2Cpp reliability
                var snapshot = builder.CaptureData();
                float? relationDelta = snapshot?.RelationDelta;
                bool? unlocked = snapshot?.Unlocked;
                NPCRelationship.UnlockType? unlockType = snapshot?.UnlockType;
                List<string> connectionIDs = snapshot?.ConnectionIDs != null && snapshot.ConnectionIDs.Count > 0
                    ? new List<string>(snapshot.ConnectionIDs)
                    : null;

                // Get or create identity data entry
                if (!_registry.TryGetValue(normalizedName, out var existingData))
                {
                    existingData = new IdentityData();
                }

                // Update relationship fields (merge with existing data)
                var updatedData = existingData;
                if (relationDelta.HasValue)
                    updatedData.RelationDelta = relationDelta;
                if (unlocked.HasValue)
                    updatedData.Unlocked = unlocked;
                if (unlockType.HasValue)
                    updatedData.UnlockType = (int?)unlockType.Value;
                if (connectionIDs != null && connectionIDs.Count > 0)
                    updatedData.ConnectionIDs = new List<string>(connectionIDs);

                _registry[normalizedName] = updatedData;
            }
            catch (Exception ex)
            {
                Logger.Error($"[Relationship Data] RegisterRelationshipDataToStaticCache: Exception storing relationship data for prefab '{prefabName}': {ex.Message}");
            }
        }

        /// <summary>
        /// INTERNAL: Called by NPCPrefabBuilder to register identity data for Il2Cpp.
        /// On Mono this is unnecessary as fields auto-serialize, but on Il2Cpp we need
        /// a static registry to survive network instantiation.
        /// </summary>
#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        internal void RegisterToStaticCache(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return;

            // Only register prefab data during Menu scene configuration; runtime instances in Main should not alter the registry.
            if (!DeferredMapResolver.IsMenuScene())
            {
                return;
            }

            // Normalize prefab name (remove "(Clone)" suffix) - only register prefabs, not spawned instances
            string normalizedName = prefabName;
            if (normalizedName.EndsWith("(Clone)"))
                normalizedName = normalizedName.Substring(0, normalizedName.Length - 7);

            // Don't register spawned instances - they should only read from registry
            // Check if this is a spawned instance by checking if gameObject name has "(Clone)"
            bool isSpawnedInstance = gameObject.name.EndsWith("(Clone)");
            if (isSpawnedInstance)
            {
                // Spawned instances should not create registry entries
                // They should only read from existing prefab entries
                return;
            }

            var avatarData = CaptureAvatarSettings(AppearanceDefaults);
            if (avatarData != null)
                avatarData.ImpostorSelection = AppearanceImpostorSelection;
            _cachedAppearanceDefaults = CloneAvatarSettingsData(avatarData);

            // Preserve existing relationship data from registry if component fields aren't set
            var relationDelta = this.RelationDelta;
            var unlocked = this.Unlocked;
            var unlockType = this.UnlockType.HasValue ? (int?)this.UnlockType.Value : null;
            PrefabName = normalizedName;
            
            // CRITICAL: Always check registry FIRST for connection IDs since they're set via RegisterRelationshipDataToStaticCache
            // Component field (_connectionIds) is never set during prefab configuration in Menu scene
            // Connection IDs are only stored via RegisterRelationshipDataToStaticCache, so we must preserve them from registry
            List<string> connectionIDs = null;
            string dealerHomeBuildingName = this.DealerHomeBuildingName;
            
            if (_registry.TryGetValue(normalizedName, out var existingData))
            {
                // Use component fields if set, otherwise preserve from registry
                if (!relationDelta.HasValue && existingData.RelationDelta.HasValue)
                    relationDelta = existingData.RelationDelta;
                if (!unlocked.HasValue && existingData.Unlocked.HasValue)
                    unlocked = existingData.Unlocked;
                if (!unlockType.HasValue && existingData.UnlockType.HasValue)
                    unlockType = existingData.UnlockType;
                
                // Preserve DealerHomeBuildingName from registry if component field is empty
                if (string.IsNullOrEmpty(dealerHomeBuildingName) && !string.IsNullOrEmpty(existingData.DealerHomeBuildingName))
                {
                    dealerHomeBuildingName = existingData.DealerHomeBuildingName;
                }
                
                // ALWAYS preserve connection IDs from registry if they exist (they come from RegisterRelationshipDataToStaticCache)
                if (existingData.ConnectionIDs != null && existingData.ConnectionIDs.Count > 0)
                {
                    connectionIDs = new List<string>(existingData.ConnectionIDs);
                }
            }
            
            // Only use component field if registry doesn't have connection IDs
            // (Component field is typically empty during prefab configuration, but check it as fallback)
            if ((connectionIDs == null || connectionIDs.Count == 0) && _connectionIds != null && _connectionIds.Count > 0)
            {
                connectionIDs = new List<string>(_connectionIds);
            }

            var identityData = new IdentityData
            {
                Id = this.Id,
                FirstName = this.FirstName,
                LastName = this.LastName,
                Icon = this.Icon,
                AppearanceDefaults = CloneAvatarSettingsData(avatarData),
                AppearanceImpostorSelection = AppearanceImpostorSelection,
                DealerHomeBuildingName = dealerHomeBuildingName,
                RelationDelta = relationDelta,
                Unlocked = unlocked,
                UnlockType = unlockType,
                ConnectionIDs = connectionIDs,
                PrefabName = normalizedName
            };

            _registry[normalizedName] = identityData;

            if (identityData.AppearanceDefaults != null)
                AppearanceDefaults = CreateAvatarSettings(identityData.AppearanceDefaults);
        }

        private void TryRestoreFromRegistry()
        {
            // Get prefab name - could be from the instance or from template
            string prefabName = gameObject.name;
            
            // Remove "(Clone)" suffix if present
            if (prefabName.EndsWith("(Clone)"))
                prefabName = prefabName.Substring(0, prefabName.Length - 7);

            // Prefer explicitly stored prefab name if available (set during prefab registration)
            if (!string.IsNullOrEmpty(PrefabName))
                prefabName = PrefabName;

            IdentityData? resolved = null;

            // Primary lookup by key
            if (_registry.TryGetValue(prefabName, out var data))
            {
                resolved = data;
            }
            else
            {
                // Fallback 1: attempt to match by stored PrefabName inside registry entries when the instance name was changed (e.g., to first name)
                foreach (var kvp in _registry)
                {
                    var entry = kvp.Value;
                    if (!string.IsNullOrEmpty(entry.PrefabName) && string.Equals(entry.PrefabName, prefabName, StringComparison.OrdinalIgnoreCase))
                    {
                        resolved = entry;
                        PrefabName = entry.PrefabName;
                        break;
                    }
                }
                
                // Fallback 2: If GameObject name was changed to first name, try to find registry entry by NPC ID
                // This handles the case where GameObject.name was changed from "S1API_ExamplePhysicalDealerNPC" to "Dealer"
                if (!resolved.HasValue)
                {
                    try
                    {
                        var npc = GetComponent<S1NPCs.NPC>();
                        if (npc != null && !string.IsNullOrEmpty(npc.ID))
                        {
                            foreach (var kvp in _registry)
                            {
                                var entry = kvp.Value;
                                // Match by ID - registry entry should have the same ID as the NPC
                                if (!string.IsNullOrEmpty(entry.Id) && string.Equals(entry.Id, npc.ID, StringComparison.OrdinalIgnoreCase))
                                {
                                    resolved = entry;
                                    PrefabName = entry.PrefabName ?? kvp.Key; // Use PrefabName from entry or fallback to registry key
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"[NPCPrefabIdentity] Exception during ID-based registry lookup: {ex.Message}");
                    }
                }
                
                // Fallback 3: If still not found and GameObject name starts with "S1API_", try direct lookup with that
                // This handles edge cases where the name wasn't changed yet
                if (!resolved.HasValue && prefabName.StartsWith("S1API_", StringComparison.OrdinalIgnoreCase))
                {
                    if (_registry.TryGetValue(prefabName, out var directData))
                    {
                        resolved = directData;
                        PrefabName = prefabName;
                    }
                }
            }

            if (resolved.HasValue)
            {
                var dataRef = resolved.Value;
                // Restore identity data (only if missing to avoid overwriting)
                if (string.IsNullOrEmpty(this.Id) && !string.IsNullOrEmpty(dataRef.Id))
                    this.Id = dataRef.Id;
                if (string.IsNullOrEmpty(this.FirstName) && !string.IsNullOrEmpty(dataRef.FirstName))
                    this.FirstName = dataRef.FirstName;
                if (string.IsNullOrEmpty(this.LastName) && !string.IsNullOrEmpty(dataRef.LastName))
                    this.LastName = dataRef.LastName;
                if (this.Icon == null && dataRef.Icon != null)
                    this.Icon = dataRef.Icon;
                // Always restore DealerHomeBuildingName from registry if component field is empty (similar to relationship data)
                if (string.IsNullOrEmpty(this.DealerHomeBuildingName) && !string.IsNullOrEmpty(dataRef.DealerHomeBuildingName))
                    this.DealerHomeBuildingName = dataRef.DealerHomeBuildingName;
                
                // Always restore relationship data from registry (these get wiped on Il2Cpp)
                this.RelationDelta = dataRef.RelationDelta;
                this.Unlocked = dataRef.Unlocked;
                this.UnlockType = dataRef.UnlockType.HasValue ? (NPCRelationship.UnlockType?)dataRef.UnlockType.Value : null;
                _connectionIds = dataRef.ConnectionIDs != null ? new List<string>(dataRef.ConnectionIDs) : null;
                PrefabName = dataRef.PrefabName ?? PrefabName;
                if (AppearanceImpostorSelection == null)
                    AppearanceImpostorSelection = dataRef.AppearanceImpostorSelection ?? dataRef.AppearanceDefaults?.ImpostorSelection;
                
                // Debug log for connection restoration
                // Silent when restored; applied logs happen later during Apply
                
                // Restore appearance defaults
                if (this.AppearanceDefaults == null)
                {
                    _cachedAppearanceDefaults = CloneAvatarSettingsData(dataRef.AppearanceDefaults);
                    if (_cachedAppearanceDefaults != null)
                        this.AppearanceDefaults = CreateAvatarSettings(_cachedAppearanceDefaults);
                    else
                        this.AppearanceDefaults = null;
                }
            }
        }
        
#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        private IEnumerator DelayedApply()
        {
            float start = Time.realtimeSinceStartup;
            float timeout = 3f;
            while (!_applied && (Time.realtimeSinceStartup - start) < timeout)
            {
                TryApplyNow();
                if (_applied)
                    yield break;
                yield return new WaitForSeconds(0.05f);
            }
        }

        /// <summary>
        /// INTERNAL: Ensures relationship data fields are populated from registry on Il2Cpp.
        /// </summary>
#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        private void EnsureRelationshipDataFromRegistry()
        {
            // Always try to restore to ensure fields are populated (Il2Cpp wipes component fields).
            if (_connectionIds == null || _connectionIds.Count == 0 || !Unlocked.HasValue || !RelationDelta.HasValue || !UnlockType.HasValue)
            {
                TryRestoreFromRegistry();
            }
        }

        /// <summary>
        /// INTERNAL: Ensures DealerHomeBuildingName is populated from registry on Il2Cpp.
        /// Uses the same prefab name lookup logic as TryRestoreFromRegistry for consistency.
        /// </summary>
#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        private void EnsureDealerHomeBuildingNameFromRegistry()
        {
            // Always try to restore to ensure field is populated (Il2Cpp wipes component fields).
            if (string.IsNullOrEmpty(DealerHomeBuildingName))
            {
                // Get prefab name using same logic as TryRestoreFromRegistry
                string prefabName = gameObject.name;
                if (prefabName.EndsWith("(Clone)"))
                    prefabName = prefabName.Substring(0, prefabName.Length - 7);
                if (!string.IsNullOrEmpty(PrefabName))
                    prefabName = PrefabName;
                
                IdentityData? resolved = null;
                
                // Primary lookup by key
                if (_registry.TryGetValue(prefabName, out var data))
                {
                    resolved = data;
                }
                else
                {
                    // Fallback 1: attempt to match by stored PrefabName inside registry entries
                    foreach (var kvp in _registry)
                    {
                        var entry = kvp.Value;
                        if (!string.IsNullOrEmpty(entry.PrefabName) && string.Equals(entry.PrefabName, prefabName, StringComparison.OrdinalIgnoreCase))
                        {
                            resolved = entry;
                            PrefabName = entry.PrefabName;
                            break;
                        }
                    }
                    
                    // Fallback 2: Try to find by NPC ID (handles case where GameObject name was changed to first name)
                    if (!resolved.HasValue)
                    {
                        try
                        {
                            var npc = GetComponent<S1NPCs.NPC>();
                            if (npc != null && !string.IsNullOrEmpty(npc.ID))
                            {
                                foreach (var kvp in _registry)
                                {
                                    var entry = kvp.Value;
                                    if (!string.IsNullOrEmpty(entry.Id) && string.Equals(entry.Id, npc.ID, StringComparison.OrdinalIgnoreCase))
                                    {
                                        resolved = entry;
                                        PrefabName = entry.PrefabName ?? kvp.Key;
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                
                if (resolved.HasValue && !string.IsNullOrEmpty(resolved.Value.DealerHomeBuildingName))
                {
                    DealerHomeBuildingName = resolved.Value.DealerHomeBuildingName;
                }
            }
        }

        /// <summary>
        /// Apply stored relationship defaults to a base-game NPC's relation data.
        /// Safe to call on both server and clients.
        /// </summary>
        internal void ApplyRelationshipDataTo(S1NPCs.NPC npc, bool preserveUnlockState = false)
        {
            if (npc == null)
                return;

            // Ensure fields are populated from registry before applying
            EnsureRelationshipDataFromRegistry();

            var relationData = npc.RelationData;
            if (relationData == null)
                return;

            try
            {
                var builder = new NPCRelationshipDataBuilder();

                if (RelationDelta.HasValue)
                    builder.WithDelta(RelationDelta.Value);

                if (Unlocked.HasValue)
                    builder.SetUnlocked(Unlocked.Value);

                if (UnlockType.HasValue)
                    builder.SetUnlockType(UnlockType.Value);

                if (_connectionIds != null && _connectionIds.Count > 0)
                {
                    builder.WithConnectionsById(_connectionIds);
                }

                builder.ApplyTo(relationData, npc, preserveUnlockState);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Relationship Data] ApplyRelationshipDataTo: Exception applying relationship data to NPC '{npc?.ID ?? "<null>"}': {ex.Message}");
            }
        }

        /// <summary>
        /// Apply stored defaults to a base-game NPC instance.
        /// Safe to call on both server and clients.
        /// </summary>
        internal void ApplyTo(S1NPCs.NPC npc)
        {
            if (npc == null)
                return;

            // Ensure identity and relationship data are populated from registry if missing
            if (string.IsNullOrEmpty(Id) && string.IsNullOrEmpty(FirstName))
            {
                TryRestoreFromRegistry();
            }
            EnsureRelationshipDataFromRegistry();
            EnsureDealerHomeBuildingNameFromRegistry();

            try {
                if (!string.IsNullOrEmpty(FirstName))
                    npc.FirstName = FirstName;
            }
            catch { }
            try
            {
                if (!string.IsNullOrEmpty(LastName))
                    npc.LastName = LastName;
            }
            catch { }
            try
            {
                if (!string.IsNullOrEmpty(Id))
                    npc.ID = Id;
            }
            catch { }
            try
            {
                if (Icon != null)
                    npc.MugshotSprite = Icon;
            }
            catch { }

            try
            {
                var avatar = npc.Avatar ?? npc.GetComponentInChildren<S1AvatarFramework.Avatar>(true);
                if (avatar != null && AppearanceDefaults != null)
                {
                    EnsureAppearanceImpostorTexture(npc.ID ?? PrefabName ?? gameObject.name);
                    avatar.LoadAvatarSettings(AppearanceDefaults);
                }
            }
            catch { }

            // Apply dealer home building if set (resolve in Main scene)
            // Always check registry in case component field is null on Il2Cpp
            try
            {
                string buildingName = DealerHomeBuildingName;
                
                // If component field is empty, try to get from registry using multiple fallback strategies
                if (string.IsNullOrEmpty(buildingName))
                {
                    string prefabName = gameObject.name;
                    if (prefabName.EndsWith("(Clone)"))
                        prefabName = prefabName.Substring(0, prefabName.Length - 7);
                    if (!string.IsNullOrEmpty(PrefabName))
                        prefabName = PrefabName;
                    
                    IdentityData? resolved = null;
                    
                    // Try direct lookup
                    if (_registry.TryGetValue(prefabName, out var data))
                    {
                        resolved = data;
                    }
                    else
                    {
                        // Fallback: Try to find by NPC ID
                        try
                        {
                            if (npc != null && !string.IsNullOrEmpty(npc.ID))
                            {
                                foreach (var kvp in _registry)
                                {
                                    var entry = kvp.Value;
                                    if (!string.IsNullOrEmpty(entry.Id) && string.Equals(entry.Id, npc.ID, StringComparison.OrdinalIgnoreCase))
                                    {
                                        resolved = entry;
                                        PrefabName = entry.PrefabName ?? kvp.Key;
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    
                    if (resolved.HasValue && !string.IsNullOrEmpty(resolved.Value.DealerHomeBuildingName))
                    {
                        buildingName = resolved.Value.DealerHomeBuildingName;
                        DealerHomeBuildingName = buildingName; // Update component field for future use
                    }
                }
                
                if (!string.IsNullOrEmpty(buildingName))
                {
                    ApplyDealerHomeBuilding(npc, buildingName);
                }
            }
            catch { }
        }

#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        private void ApplyDealerHomeBuilding(S1NPCs.NPC npc, string buildingName = null)
        {
            // Use provided building name or fall back to component field
            if (string.IsNullOrEmpty(buildingName))
                buildingName = DealerHomeBuildingName;
            
            // Only resolve in Main scene where buildings are available
            if (DeferredMapResolver.IsMenuScene())
            {
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(buildingName))
                {
                    Logger.Warning($"[Dealer Home] Building name is empty for NPC {npc?.ID ?? "<null>"}");
                    return;
                }

                var dealerComponent = npc.GetComponent<S1Economy.Dealer>();
                if (dealerComponent == null)
                {
                    Logger.Warning($"[Dealer Home] Dealer component not found for NPC {npc?.ID ?? "<null>"}");
                    return;
                }

                // Try to get building wrapper by name
                var building = global::S1API.Map.Building.GetByName(buildingName);
                if (building == null)
                {
                    Logger.Warning($"[Dealer Home] Building '{buildingName}' not found in registry for NPC {npc?.ID ?? "<null>"}");
                    return;
                }

                var gameBuilding = building.ResolveGameBuilding();
                if (gameBuilding == null)
                {
                    Logger.Warning($"[Dealer Home] Building '{buildingName}' wrapper found but game building is null for NPC {npc?.ID ?? "<null>"}");
                    return;
                }

                bool success = ReflectionUtils.TrySetFieldOrProperty(dealerComponent, "Home", gameBuilding);
                if (!success)
                {
                    Logger.Warning($"[Dealer Home] Failed to set Home property on Dealer component for NPC {npc?.ID ?? "<null>"}");
                }
                else
                {
                    // Dealer.Awake() runs HomeEvent.Building = Home, but for S1API NPCs
                    // Home is null at Awake-time (resolved later). We must sync HomeEvent.Building
                    // so the schedule system knows where to send the dealer.
                    try
                    {
#if MONOMELON
                        var homeEventField = typeof(S1Economy.Dealer).GetField("HomeEvent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
#else
                        var homeEventField = typeof(S1Economy.Dealer).GetProperty("HomeEvent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
#endif
                        var homeEvent = homeEventField?.GetValue(dealerComponent);
                        if (homeEvent != null)
                        {
                            ReflectionUtils.TrySetFieldOrProperty(homeEvent, "Building", gameBuilding);
                        }
                    }
                    catch (Exception homeEventEx)
                    {
                        Logger.Warning($"[Dealer Home] Failed to sync HomeEvent.Building for NPC {npc?.ID ?? "<null>"}: {homeEventEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Dealer Home] Exception applying dealer home building for NPC {npc?.ID ?? "<null>"}: {ex.Message}");
                Logger.Error($"[Dealer Home] Stack trace: {ex.StackTrace}");
            }
        }

        private void TryApplyNow()
        {
            try
            {
                var npc = GetComponent<S1NPCs.NPC>();
                if (npc == null)
                    return;
                EnsureAppearanceDefaults();
                ApplyTo(npc);
                // Consider applied once avatar exists or when only identity fields are requested
                var avatar = npc.Avatar ?? npc.GetComponentInChildren<S1AvatarFramework.Avatar>(true);
                _applied = (AppearanceDefaults == null) || (avatar != null);
            }
            catch { }
        }

        private void EnsureAppearanceDefaults()
        {
            if (AppearanceDefaults != null)
                return;

            if (_cachedAppearanceDefaults != null)
            {
                AppearanceDefaults = CreateAvatarSettings(_cachedAppearanceDefaults);
                EnsureAppearanceImpostorTexture(PrefabName ?? gameObject.name);
                return;
            }

            if (TryGetRegistryData(out var data) && data.AppearanceDefaults != null)
            {
                _cachedAppearanceDefaults = CloneAvatarSettingsData(data.AppearanceDefaults);
                if (AppearanceImpostorSelection == null)
                    AppearanceImpostorSelection = data.AppearanceImpostorSelection ?? data.AppearanceDefaults?.ImpostorSelection;
                AppearanceDefaults = CreateAvatarSettings(_cachedAppearanceDefaults);
                EnsureAppearanceImpostorTexture(PrefabName ?? gameObject.name);
            }
        }

#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        private void EnsureAppearanceImpostorTexture(string deterministicKey)
        {
            if (AppearanceDefaults == null || AppearanceDefaults.ImpostorTexture != null)
                return;

            var selection =
                _cachedAppearanceDefaults?.ImpostorSelection ??
                AppearanceImpostorSelection;

            if (selection == null || selection.Kind == AvatarImpostorSelectionKind.Preserve)
                return;

            if (ImpostorTextureResolver.TryResolve(selection, deterministicKey, out Texture2D? texture) && texture != null)
            {
                AppearanceDefaults.ImpostorTexture = texture;
                if (_cachedAppearanceDefaults != null)
                    _cachedAppearanceDefaults.ImpostorTexture = texture;
            }
        }

#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        private bool TryGetRegistryData(out IdentityData data)
        {
            string? prefabName = PrefabName;
            if (string.IsNullOrEmpty(prefabName))
                prefabName = gameObject.name;

            if (string.IsNullOrEmpty(prefabName))
            {
                data = default;
                return false;
            }

            if (prefabName.EndsWith("(Clone)"))
                prefabName = prefabName.Substring(0, prefabName.Length - 7);

            if (_registry.TryGetValue(prefabName, out data))
                return true;

            try
            {
                var npc = GetComponent<S1NPCs.NPC>();
                if (npc != null && !string.IsNullOrEmpty(npc.ID))
                {
                    foreach (var kvp in _registry)
                    {
                        var entry = kvp.Value;
                        if (!string.IsNullOrEmpty(entry.Id) && string.Equals(entry.Id, npc.ID, StringComparison.OrdinalIgnoreCase))
                        {
                            data = entry;
                            PrefabName = entry.PrefabName ?? kvp.Key;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }

            data = default;
            return false;
        }

        /// <summary>
        /// INTERNAL: Retrieves identity data from the static registry by prefab name.
        /// Used by NPC constructor on Il2Cpp to read identity before Awake() is called.
        /// </summary>
#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        internal static bool TryGetIdentityFromRegistry(string prefabName, out string id, out string firstName, out string lastName, out Sprite icon)
        {
            id = null;
            firstName = null;
            lastName = null;
            icon = null;

            if (string.IsNullOrEmpty(prefabName))
                return false;

            // Remove "(Clone)" suffix if present
            if (prefabName.EndsWith("(Clone)"))
                prefabName = prefabName.Substring(0, prefabName.Length - 7);

            if (_registry.TryGetValue(prefabName, out var data))
            {
                id = data.Id;
                firstName = data.FirstName;
                lastName = data.LastName;
                icon = data.Icon;
                return true;
            }

            return false;
        }

        /// <summary>
        /// INTERNAL: Retrieves relationship data from the static registry by prefab name.
        /// Used to read relationship defaults before Awake() is called on Il2Cpp.
        /// </summary>
#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        internal static bool TryGetRelationshipDataFromRegistry(string prefabName, out float? relationDelta, out bool? unlocked, out NPCRelationship.UnlockType? unlockType, out List<string> connectionIDs)
        {
            relationDelta = null;
            unlocked = null;
            unlockType = null;
            connectionIDs = null;

            if (string.IsNullOrEmpty(prefabName))
                return false;

            // Remove "(Clone)" suffix if present
            if (prefabName.EndsWith("(Clone)"))
                prefabName = prefabName.Substring(0, prefabName.Length - 7);

            if (_registry.TryGetValue(prefabName, out var data))
            {
                relationDelta = data.RelationDelta;
                unlocked = data.Unlocked;
                unlockType = data.UnlockType.HasValue ? (NPCRelationship.UnlockType?)data.UnlockType.Value : null;
                connectionIDs = data.ConnectionIDs != null ? new List<string>(data.ConnectionIDs) : null;
                return true;
            }

            return false;
        }

#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        private static AvatarSettingsData CaptureAvatarSettings(S1AvatarFramework.AvatarSettings settings)
        {
            if (settings == null)
                return null;

            var data = new AvatarSettingsData
            {
                Gender = settings.Gender,
                Height = settings.Height,
                Weight = settings.Weight,
                SkinColor = settings.SkinColor,
                EyeBallTint = settings.EyeBallTint,
                LeftEyeLidColor = settings.LeftEyeLidColor,
                RightEyeLidColor = settings.RightEyeLidColor,
                EyeballMaterialIdentifier = settings.EyeballMaterialIdentifier,
                PupilDilation = settings.PupilDilation,
                EyebrowScale = settings.EyebrowScale,
                EyebrowThickness = settings.EyebrowThickness,
                EyebrowRestingHeight = settings.EyebrowRestingHeight,
                EyebrowRestingAngle = settings.EyebrowRestingAngle,
                HairPath = settings.HairPath,
                HairColor = settings.HairColor,
                ImpostorTexture = settings.ImpostorTexture,
                LeftEye = new EyeStateData
                {
                    TopLidOpen = settings.LeftEyeRestingState.topLidOpen,
                    BottomLidOpen = settings.LeftEyeRestingState.bottomLidOpen
                },
                RightEye = new EyeStateData
                {
                    TopLidOpen = settings.RightEyeRestingState.topLidOpen,
                    BottomLidOpen = settings.RightEyeRestingState.bottomLidOpen
                }
            };

            if (settings.FaceLayerSettings != null)
            {
                for (int i = 0; i < settings.FaceLayerSettings.Count; i++)
                {
                    var layer = settings.FaceLayerSettings[i];
                    data.FaceLayers.Add(new LayerSettingData
                    {
                        Path = layer.layerPath,
                        Color = layer.layerTint
                    });
                }
            }

            if (settings.BodyLayerSettings != null)
            {
                for (int i = 0; i < settings.BodyLayerSettings.Count; i++)
                {
                    var layer = settings.BodyLayerSettings[i];
                    data.BodyLayers.Add(new LayerSettingData
                    {
                        Path = layer.layerPath,
                        Color = layer.layerTint
                    });
                }
            }

            if (settings.AccessorySettings != null)
            {
                for (int i = 0; i < settings.AccessorySettings.Count; i++)
                {
                    var accessory = settings.AccessorySettings[i];
                    data.Accessories.Add(new AccessorySettingData
                    {
                        Path = accessory.path,
                        Color = accessory.color
                    });
                }
            }

            return data;
        }

#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        private static AvatarSettingsData CloneAvatarSettingsData(AvatarSettingsData source)
        {
            if (source == null)
                return null;

            var clone = new AvatarSettingsData
            {
                Gender = source.Gender,
                Height = source.Height,
                Weight = source.Weight,
                SkinColor = source.SkinColor,
                EyeBallTint = source.EyeBallTint,
                PupilDilation = source.PupilDilation,
                EyebrowScale = source.EyebrowScale,
                EyebrowThickness = source.EyebrowThickness,
                EyebrowRestingHeight = source.EyebrowRestingHeight,
                EyebrowRestingAngle = source.EyebrowRestingAngle,
                HairPath = source.HairPath,
                HairColor = source.HairColor,
                ImpostorTexture = source.ImpostorTexture,
                ImpostorSelection = source.ImpostorSelection,
                LeftEyeLidColor = source.LeftEyeLidColor,
                RightEyeLidColor = source.RightEyeLidColor,
                EyeballMaterialIdentifier = source.EyeballMaterialIdentifier,
                LeftEye = new EyeStateData
                {
                    TopLidOpen = source.LeftEye.TopLidOpen,
                    BottomLidOpen = source.LeftEye.BottomLidOpen
                },
                RightEye = new EyeStateData
                {
                    TopLidOpen = source.RightEye.TopLidOpen,
                    BottomLidOpen = source.RightEye.BottomLidOpen
                }
            };

            for (int i = 0; i < source.FaceLayers.Count; i++)
            {
                var layer = source.FaceLayers[i];
                clone.FaceLayers.Add(new LayerSettingData { Path = layer.Path, Color = layer.Color });
            }

            for (int i = 0; i < source.BodyLayers.Count; i++)
            {
                var layer = source.BodyLayers[i];
                clone.BodyLayers.Add(new LayerSettingData { Path = layer.Path, Color = layer.Color });
            }

            for (int i = 0; i < source.Accessories.Count; i++)
            {
                var accessory = source.Accessories[i];
                clone.Accessories.Add(new AccessorySettingData { Path = accessory.Path, Color = accessory.Color });
            }

            return clone;
        }

#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        private static S1AvatarFramework.AvatarSettings CreateAvatarSettings(AvatarSettingsData data)
        {
            if (data == null)
                return null;

            var settings = ScriptableObject.CreateInstance<S1AvatarFramework.AvatarSettings>();
            settings.hideFlags = HideFlags.DontUnloadUnusedAsset;

            settings.Gender = data.Gender;
            settings.Height = data.Height;
            settings.Weight = data.Weight;
            settings.SkinColor = data.SkinColor;
            settings.LeftEyeLidColor = data.LeftEyeLidColor;
            settings.RightEyeLidColor = data.RightEyeLidColor;
            settings.EyeBallTint = data.EyeBallTint;
            settings.EyeballMaterialIdentifier = data.EyeballMaterialIdentifier;
            settings.PupilDilation = data.PupilDilation;
            settings.EyebrowScale = data.EyebrowScale;
            settings.EyebrowThickness = data.EyebrowThickness;
            settings.EyebrowRestingHeight = data.EyebrowRestingHeight;
            settings.EyebrowRestingAngle = data.EyebrowRestingAngle;
            settings.HairPath = data.HairPath ?? string.Empty;
            settings.HairColor = data.HairColor;
            settings.ImpostorTexture = data.ImpostorTexture;
            settings.LeftEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
            {
                topLidOpen = data.LeftEye.TopLidOpen,
                bottomLidOpen = data.LeftEye.BottomLidOpen
            };
            settings.RightEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
            {
                topLidOpen = data.RightEye.TopLidOpen,
                bottomLidOpen = data.RightEye.BottomLidOpen
            };

            var faceLayers = new List<S1AvatarFramework.AvatarSettings.LayerSetting>();
            for (int i = 0; i < data.FaceLayers.Count; i++)
            {
                var layer = data.FaceLayers[i];
                faceLayers.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
                {
                    layerPath = layer.Path,
                    layerTint = layer.Color
                });
            }

            var bodyLayers = new List<S1AvatarFramework.AvatarSettings.LayerSetting>();
            for (int i = 0; i < data.BodyLayers.Count; i++)
            {
                var layer = data.BodyLayers[i];
                bodyLayers.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
                {
                    layerPath = layer.Path,
                    layerTint = layer.Color
                });
            }

            var accessories = new List<S1AvatarFramework.AvatarSettings.AccessorySetting>();
            for (int i = 0; i < data.Accessories.Count; i++)
            {
                var accessory = data.Accessories[i];
                accessories.Add(new S1AvatarFramework.AvatarSettings.AccessorySetting
                {
                    path = accessory.Path,
                    color = accessory.Color
                });
            }

            settings.FaceLayerSettings = ToIl2CppList(faceLayers);
            settings.BodyLayerSettings = ToIl2CppList(bodyLayers);
            settings.AccessorySettings = ToIl2CppList(accessories);

            return settings;
        }

#if IL2CPPMELON
        [HideFromIl2Cpp]
        private static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(List<T> source)
        {
            var list = new Il2CppSystem.Collections.Generic.List<T>();
            if (source == null)
                return list;
            for (int i = 0; i < source.Count; i++)
                list.Add(source[i]);
            return list;
        }
#else
        private static List<T> ToIl2CppList<T>(List<T> source)
        {
            return source ?? new List<T>();
        }
#endif

        private sealed class AvatarSettingsData
        {
            internal float Gender;
            internal float Height;
            internal float Weight;
            internal Color32 SkinColor;
            internal Color EyeBallTint;
            internal float PupilDilation;
            internal float EyebrowScale;
            internal float EyebrowThickness;
            internal float EyebrowRestingHeight;
            internal float EyebrowRestingAngle;
            internal string HairPath;
            internal Color HairColor;
            internal Texture2D ImpostorTexture;
            internal AvatarImpostorSelection ImpostorSelection;
            internal Color LeftEyeLidColor;
            internal Color RightEyeLidColor;
            internal string EyeballMaterialIdentifier;
            internal EyeStateData LeftEye = new EyeStateData();
            internal EyeStateData RightEye = new EyeStateData();
            internal List<LayerSettingData> FaceLayers = new List<LayerSettingData>();
            internal List<LayerSettingData> BodyLayers = new List<LayerSettingData>();
            internal List<AccessorySettingData> Accessories = new List<AccessorySettingData>();
        }

        private sealed class EyeStateData
        {
            internal float TopLidOpen;
            internal float BottomLidOpen;
        }

        private sealed class LayerSettingData
        {
            internal string Path;
            internal Color Color;
        }

        private sealed class AccessorySettingData
        {
            internal string Path;
            internal Color Color;
        }
    }
}



