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
    public sealed class NPCPrefabIdentity : MonoBehaviour
    {
        // Public fields for Mono compatibility (auto-serialized there)
        public string Id;
        public string FirstName;
        public string LastName;
        public Sprite Icon;
        public S1AvatarFramework.AvatarSettings AppearanceDefaults;
        public string DealerHomeBuildingName;

        // Relationship data fields for Mono compatibility
        public float? RelationDelta;
        public bool? Unlocked;
        public NPCRelationship.UnlockType? UnlockType;
        public List<string> ConnectionIDs;

        // Static registry to preserve data across network instantiation on Il2Cpp
        private static readonly Dictionary<string, IdentityData> _registry = new Dictionary<string, IdentityData>();
        private bool _applied;
        private AvatarSettingsData _cachedAppearanceDefaults;

        private struct IdentityData
        {
            public string Id;
            public string FirstName;
            public string LastName;
            public Sprite Icon;
            public AvatarSettingsData AppearanceDefaults;
            public string DealerHomeBuildingName;
            public float? RelationDelta;
            public bool? Unlocked;
            public int? UnlockType; // Stored as int (0=Recommendation, 1=DirectApproach) to avoid enum dependency
            public List<string> ConnectionIDs;
        }

        private void Awake()
        {
            // On Il2Cpp, restore fields from registry if this is a spawned instance
            #if IL2CPPMELON
            TryRestoreFromRegistry();
            #endif
        }

        private void Start()
        {
            // On Il2Cpp, restore fields from registry again in Start() in case they were wiped after Awake()
#if IL2CPPMELON
            TryRestoreFromRegistry();
#endif
            // Best-effort: apply immediately, then retry briefly in case Avatar isn't yet available on clients.
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
            catch { }
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
            _cachedAppearanceDefaults = CloneAvatarSettingsData(avatarData);

            // Preserve existing relationship data from registry if component fields aren't set
            var relationDelta = this.RelationDelta;
            var unlocked = this.Unlocked;
            var unlockType = this.UnlockType.HasValue ? (int?)this.UnlockType.Value : null;
            List<string> connectionIDs = this.ConnectionIDs != null ? new List<string>(this.ConnectionIDs) : null;
            
            if (_registry.TryGetValue(normalizedName, out var existingData))
            {
                // Use component fields if set, otherwise preserve from registry
                if (!relationDelta.HasValue && existingData.RelationDelta.HasValue)
                    relationDelta = existingData.RelationDelta;
                if (!unlocked.HasValue && existingData.Unlocked.HasValue)
                    unlocked = existingData.Unlocked;
                if (!unlockType.HasValue && existingData.UnlockType.HasValue)
                    unlockType = existingData.UnlockType;
                if ((connectionIDs == null || connectionIDs.Count == 0) && existingData.ConnectionIDs != null && existingData.ConnectionIDs.Count > 0)
                    connectionIDs = new List<string>(existingData.ConnectionIDs);
            }

            var identityData = new IdentityData
            {
                Id = this.Id,
                FirstName = this.FirstName,
                LastName = this.LastName,
                Icon = this.Icon,
                AppearanceDefaults = CloneAvatarSettingsData(avatarData),
                DealerHomeBuildingName = this.DealerHomeBuildingName,
                RelationDelta = relationDelta,
                Unlocked = unlocked,
                UnlockType = unlockType,
                ConnectionIDs = connectionIDs
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

            if (_registry.TryGetValue(prefabName, out var data))
            {
                // Restore identity data (only if missing to avoid overwriting)
                if (string.IsNullOrEmpty(this.Id) && !string.IsNullOrEmpty(data.Id))
                    this.Id = data.Id;
                if (string.IsNullOrEmpty(this.FirstName) && !string.IsNullOrEmpty(data.FirstName))
                    this.FirstName = data.FirstName;
                if (string.IsNullOrEmpty(this.LastName) && !string.IsNullOrEmpty(data.LastName))
                    this.LastName = data.LastName;
                if (this.Icon == null && data.Icon != null)
                    this.Icon = data.Icon;
                if (string.IsNullOrEmpty(this.DealerHomeBuildingName) && !string.IsNullOrEmpty(data.DealerHomeBuildingName))
                    this.DealerHomeBuildingName = data.DealerHomeBuildingName;
                
                // Always restore relationship data from registry (these get wiped on Il2Cpp)
                this.RelationDelta = data.RelationDelta;
                this.Unlocked = data.Unlocked;
                this.UnlockType = data.UnlockType.HasValue ? (NPCRelationship.UnlockType?)data.UnlockType.Value : null;
                this.ConnectionIDs = data.ConnectionIDs != null ? new List<string>(data.ConnectionIDs) : null;
                
                // Restore appearance defaults
                if (this.AppearanceDefaults == null)
                {
                    _cachedAppearanceDefaults = CloneAvatarSettingsData(data.AppearanceDefaults);
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
            // Always try to restore on Il2Cpp to ensure fields are populated
#if IL2CPPMELON
            TryRestoreFromRegistry();
#endif
        }

        /// <summary>
        /// Apply stored relationship defaults to a base-game NPC's relation data.
        /// Safe to call on both server and clients.
        /// </summary>
        public void ApplyRelationshipDataTo(S1NPCs.NPC npc, bool preserveUnlockState = false)
        {
            if (npc == null)
                return;

            // On Il2Cpp, ensure fields are populated from registry
#if IL2CPPMELON
            EnsureRelationshipDataFromRegistry();
#endif

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

                if (ConnectionIDs != null && ConnectionIDs.Count > 0)
                    builder.WithConnectionsById(ConnectionIDs);

                builder.ApplyTo(relationData, npc, preserveUnlockState);
            }
            catch { }
        }

        /// <summary>
        /// Apply stored defaults to a base-game NPC instance.
        /// Safe to call on both server and clients.
        /// </summary>
        public void ApplyTo(S1NPCs.NPC npc)
        {
            if (npc == null)
                return;

            // On Il2Cpp, ensure fields are populated from registry if they're empty
#if IL2CPPMELON
            if (string.IsNullOrEmpty(Id) && string.IsNullOrEmpty(FirstName))
            {
                TryRestoreFromRegistry();
            }
            // Also ensure relationship data is restored
            EnsureRelationshipDataFromRegistry();
#endif

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
                    avatar.LoadAvatarSettings(AppearanceDefaults);
                }
            }
            catch { }

            // Apply dealer home building if set (resolve in Main scene)
            try
            {
                if (!string.IsNullOrEmpty(DealerHomeBuildingName))
                {
                    ApplyDealerHomeBuilding(npc);
                }
            }
            catch { }
        }

#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        private void ApplyDealerHomeBuilding(S1NPCs.NPC npc)
        {
            // Only resolve in Main scene where buildings are available
            if (DeferredMapResolver.IsMenuScene())
                return;

            try
            {
                var dealerComponent = npc.GetComponent<S1Economy.Dealer>();
                if (dealerComponent == null)
                    return;

                // Try to get building wrapper by name
                var building = Building.GetByName(DealerHomeBuildingName);
                if (building != null)
                {
                    var gameBuilding = building.ResolveGameBuilding();
                    if (gameBuilding != null)
                    {
                        ReflectionUtils.TrySetFieldOrProperty(dealerComponent, "Home", gameBuilding);
                    }
                }
            }
            catch { }
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
                return;
            }

            if (TryGetRegistryData(out var data) && data.AppearanceDefaults != null)
            {
                _cachedAppearanceDefaults = CloneAvatarSettingsData(data.AppearanceDefaults);
                AppearanceDefaults = CreateAvatarSettings(_cachedAppearanceDefaults);
            }
        }

#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        private bool TryGetRegistryData(out IdentityData data)
        {
            string prefabName = gameObject.name;
            if (prefabName.EndsWith("(Clone)"))
                prefabName = prefabName.Substring(0, prefabName.Length - 7);

            return _registry.TryGetValue(prefabName, out data);
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
            public float Gender;
            public float Height;
            public float Weight;
            public Color32 SkinColor;
            public Color EyeBallTint;
            public float PupilDilation;
            public float EyebrowScale;
            public float EyebrowThickness;
            public float EyebrowRestingHeight;
            public float EyebrowRestingAngle;
            public string HairPath;
            public Color HairColor;
            public Color LeftEyeLidColor;
            public Color RightEyeLidColor;
            public string EyeballMaterialIdentifier;
            public EyeStateData LeftEye = new EyeStateData();
            public EyeStateData RightEye = new EyeStateData();
            public List<LayerSettingData> FaceLayers = new List<LayerSettingData>();
            public List<LayerSettingData> BodyLayers = new List<LayerSettingData>();
            public List<AccessorySettingData> Accessories = new List<AccessorySettingData>();
        }

        private sealed class EyeStateData
        {
            public float TopLidOpen;
            public float BottomLidOpen;
        }

        private sealed class LayerSettingData
        {
            public string Path;
            public Color Color;
        }

        private sealed class AccessorySettingData
        {
            public string Path;
            public Color Color;
        }
    }
}



