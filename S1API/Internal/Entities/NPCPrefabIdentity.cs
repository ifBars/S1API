#if (IL2CPPMELON)
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1NPCs = Il2CppScheduleOne.NPCs;
using Il2CppInterop.Runtime.Attributes;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1NPCs = ScheduleOne.NPCs;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MelonLoader;

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
            // Best-effort: apply immediately, then retry briefly in case Avatar isn't yet available on clients.
            TryApplyNow();
            if (!_applied)
                MelonCoroutines.Start(DelayedApply());
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

            var avatarData = CaptureAvatarSettings(AppearanceDefaults);
            _cachedAppearanceDefaults = CloneAvatarSettingsData(avatarData);

            var identityData = new IdentityData
            {
                Id = this.Id,
                FirstName = this.FirstName,
                LastName = this.LastName,
                Icon = this.Icon,
                AppearanceDefaults = CloneAvatarSettingsData(avatarData)
            };

            _registry[prefabName] = identityData;

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
                this.Id = data.Id;
                this.FirstName = data.FirstName;
                this.LastName = data.LastName;
                this.Icon = data.Icon;
                _cachedAppearanceDefaults = CloneAvatarSettingsData(data.AppearanceDefaults);
                if (_cachedAppearanceDefaults != null)
                    this.AppearanceDefaults = CreateAvatarSettings(_cachedAppearanceDefaults);
                else
                    this.AppearanceDefaults = null;
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



