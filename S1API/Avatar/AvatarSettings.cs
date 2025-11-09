#if (IL2CPPMELON)
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using Il2CppCollectionsGeneric = Il2CppSystem.Collections.Generic;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarFramework = ScheduleOne.AvatarFramework;
#endif

using System.Collections.Generic;
using UnityEngine;

namespace S1API.Avatar
{
    /// <summary>
    /// Modder-facing wrapper for AvatarSettings ScriptableObject.
    /// Provides access to avatar appearance configuration without exposing game types.
    /// </summary>
    public sealed class AvatarSettings
    {
        /// <summary>
        /// INTERNAL: Reference to the game AvatarSettings instance.
        /// </summary>
        internal readonly S1AvatarFramework.AvatarSettings S1AvatarSettings;

        /// <summary>
        /// INTERNAL: Constructor to create a wrapper from a game AvatarSettings instance.
        /// </summary>
        /// <param name="settings">The game AvatarSettings instance to wrap.</param>
        internal AvatarSettings(S1AvatarFramework.AvatarSettings settings)
        {
            S1AvatarSettings = settings;
        }

        /// <summary>
        /// Creates a new AvatarSettings instance.
        /// </summary>
        public static AvatarSettings Create()
        {
            var settings = ScriptableObject.CreateInstance<S1AvatarFramework.AvatarSettings>();
            settings.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return new AvatarSettings(settings);
        }

        /// <summary>
        /// Gender value (0.0 to 1.0).
        /// </summary>
        public float Gender
        {
            get => S1AvatarSettings?.Gender ?? 0.5f;
            set { if (S1AvatarSettings != null) S1AvatarSettings.Gender = value; }
        }

        /// <summary>
        /// Height value (typically 0.0 to 1.0).
        /// </summary>
        public float Height
        {
            get => S1AvatarSettings?.Height ?? 1.0f;
            set { if (S1AvatarSettings != null) S1AvatarSettings.Height = value; }
        }

        /// <summary>
        /// Weight value (typically 0.0 to 1.0).
        /// </summary>
        public float Weight
        {
            get => S1AvatarSettings?.Weight ?? 0.5f;
            set { if (S1AvatarSettings != null) S1AvatarSettings.Weight = value; }
        }

        /// <summary>
        /// Skin color.
        /// </summary>
        public Color32 SkinColor
        {
            get => S1AvatarSettings?.SkinColor ?? new Color32(150, 120, 95, 255);
            set { if (S1AvatarSettings != null) S1AvatarSettings.SkinColor = value; }
        }

        /// <summary>
        /// Hair path/identifier.
        /// </summary>
        public string HairPath
        {
            get => S1AvatarSettings?.HairPath ?? string.Empty;
            set { if (S1AvatarSettings != null) S1AvatarSettings.HairPath = value ?? string.Empty; }
        }

        /// <summary>
        /// Hair color.
        /// </summary>
        public Color HairColor
        {
            get => S1AvatarSettings?.HairColor ?? Color.black;
            set { if (S1AvatarSettings != null) S1AvatarSettings.HairColor = value; }
        }

        /// <summary>
        /// Left eye lid color.
        /// </summary>
        public Color32 LeftEyeLidColor
        {
            get => S1AvatarSettings?.LeftEyeLidColor ?? SkinColor;
            set { if (S1AvatarSettings != null) S1AvatarSettings.LeftEyeLidColor = value; }
        }

        /// <summary>
        /// Right eye lid color.
        /// </summary>
        public Color32 RightEyeLidColor
        {
            get => S1AvatarSettings?.RightEyeLidColor ?? SkinColor;
            set { if (S1AvatarSettings != null) S1AvatarSettings.RightEyeLidColor = value; }
        }

        /// <summary>
        /// Eye ball tint color.
        /// </summary>
        public Color EyeBallTint
        {
            get => S1AvatarSettings?.EyeBallTint ?? Color.white;
            set { if (S1AvatarSettings != null) S1AvatarSettings.EyeBallTint = value; }
        }

        /// <summary>
        /// Pupil dilation value (typically 0.0 to 1.0).
        /// </summary>
        public float PupilDilation
        {
            get => S1AvatarSettings?.PupilDilation ?? 0.5f;
            set { if (S1AvatarSettings != null) S1AvatarSettings.PupilDilation = value; }
        }

        /// <summary>
        /// Eyeball material identifier.
        /// </summary>
        public string EyeballMaterialIdentifier
        {
            get => S1AvatarSettings?.EyeballMaterialIdentifier ?? "Default";
            set { if (S1AvatarSettings != null) S1AvatarSettings.EyeballMaterialIdentifier = value ?? "Default"; }
        }

        /// <summary>
        /// Eyebrow scale value.
        /// </summary>
        public float EyebrowScale
        {
            get => S1AvatarSettings?.EyebrowScale ?? 1.0f;
            set { if (S1AvatarSettings != null) S1AvatarSettings.EyebrowScale = value; }
        }

        /// <summary>
        /// Eyebrow thickness value.
        /// </summary>
        public float EyebrowThickness
        {
            get => S1AvatarSettings?.EyebrowThickness ?? 1.0f;
            set { if (S1AvatarSettings != null) S1AvatarSettings.EyebrowThickness = value; }
        }

        /// <summary>
        /// Eyebrow resting height value.
        /// </summary>
        public float EyebrowRestingHeight
        {
            get => S1AvatarSettings?.EyebrowRestingHeight ?? 0.0f;
            set { if (S1AvatarSettings != null) S1AvatarSettings.EyebrowRestingHeight = value; }
        }

        /// <summary>
        /// Eyebrow resting angle value.
        /// </summary>
        public float EyebrowRestingAngle
        {
            get => S1AvatarSettings?.EyebrowRestingAngle ?? 0.0f;
            set { if (S1AvatarSettings != null) S1AvatarSettings.EyebrowRestingAngle = value; }
        }

        /// <summary>
        /// Left eye resting state configuration.
        /// </summary>
        public EyeLidConfiguration LeftEyeRestingState
        {
            get
            {
                if (S1AvatarSettings?.LeftEyeRestingState == null)
                    return new EyeLidConfiguration { TopLidOpen = 0.5f, BottomLidOpen = 0.5f };
                
                return new EyeLidConfiguration
                {
                    TopLidOpen = S1AvatarSettings.LeftEyeRestingState.topLidOpen,
                    BottomLidOpen = S1AvatarSettings.LeftEyeRestingState.bottomLidOpen
                };
            }
            set
            {
                if (S1AvatarSettings != null && value != null)
                {
                    S1AvatarSettings.LeftEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
                    {
                        topLidOpen = value.TopLidOpen,
                        bottomLidOpen = value.BottomLidOpen
                    };
                }
            }
        }

        /// <summary>
        /// Right eye resting state configuration.
        /// </summary>
        public EyeLidConfiguration RightEyeRestingState
        {
            get
            {
                if (S1AvatarSettings?.RightEyeRestingState == null)
                    return new EyeLidConfiguration { TopLidOpen = 0.5f, BottomLidOpen = 0.5f };
                
                return new EyeLidConfiguration
                {
                    TopLidOpen = S1AvatarSettings.RightEyeRestingState.topLidOpen,
                    BottomLidOpen = S1AvatarSettings.RightEyeRestingState.bottomLidOpen
                };
            }
            set
            {
                if (S1AvatarSettings != null && value != null)
                {
                    S1AvatarSettings.RightEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
                    {
                        topLidOpen = value.TopLidOpen,
                        bottomLidOpen = value.BottomLidOpen
                    };
                }
            }
        }

        /// <summary>
        /// Face layer settings count.
        /// </summary>
        public int FaceLayerCount => S1AvatarSettings?.FaceLayerSettings?.Count ?? 0;

        /// <summary>
        /// Body layer settings count.
        /// </summary>
        public int BodyLayerCount => S1AvatarSettings?.BodyLayerSettings?.Count ?? 0;

        /// <summary>
        /// Accessory settings count.
        /// </summary>
        public int AccessoryCount => S1AvatarSettings?.AccessorySettings?.Count ?? 0;

        /// <summary>
        /// Adds a face layer setting.
        /// </summary>
        public void AddFaceLayer(string layerPath, Color layerTint)
        {
            if (S1AvatarSettings == null || string.IsNullOrWhiteSpace(layerPath))
                return;

            if (S1AvatarSettings.FaceLayerSettings == null)
            {
#if (IL2CPPMELON)
                S1AvatarSettings.FaceLayerSettings = new Il2CppCollectionsGeneric.List<S1AvatarFramework.AvatarSettings.LayerSetting>();
#else
                S1AvatarSettings.FaceLayerSettings = new List<S1AvatarFramework.AvatarSettings.LayerSetting>();
#endif
            }

            S1AvatarSettings.FaceLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
            {
                layerPath = layerPath,
                layerTint = layerTint
            });
        }

        /// <summary>
        /// Adds a body layer setting.
        /// </summary>
        public void AddBodyLayer(string layerPath, Color layerTint)
        {
            if (S1AvatarSettings == null || string.IsNullOrWhiteSpace(layerPath))
                return;

            if (S1AvatarSettings.BodyLayerSettings == null)
            {
#if (IL2CPPMELON)
                S1AvatarSettings.BodyLayerSettings = new Il2CppCollectionsGeneric.List<S1AvatarFramework.AvatarSettings.LayerSetting>();
#else
                S1AvatarSettings.BodyLayerSettings = new List<S1AvatarFramework.AvatarSettings.LayerSetting>();
#endif
            }

            S1AvatarSettings.BodyLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
            {
                layerPath = layerPath,
                layerTint = layerTint
            });
        }

        /// <summary>
        /// Adds an accessory setting.
        /// </summary>
        public void AddAccessory(string path, Color color)
        {
            if (S1AvatarSettings == null || string.IsNullOrWhiteSpace(path))
                return;

            if (S1AvatarSettings.AccessorySettings == null)
            {
#if (IL2CPPMELON)
                S1AvatarSettings.AccessorySettings = new Il2CppCollectionsGeneric.List<S1AvatarFramework.AvatarSettings.AccessorySetting>();
#else
                S1AvatarSettings.AccessorySettings = new List<S1AvatarFramework.AvatarSettings.AccessorySetting>();
#endif
            }

            S1AvatarSettings.AccessorySettings.Add(new S1AvatarFramework.AvatarSettings.AccessorySetting
            {
                path = path,
                color = color
            });
        }

        /// <summary>
        /// Sets face layer settings from a list.
        /// </summary>
        public void SetFaceLayers(List<LayerSetting> layers)
        {
            if (S1AvatarSettings == null)
                return;

#if (IL2CPPMELON)
            var list = new Il2CppCollectionsGeneric.List<S1AvatarFramework.AvatarSettings.LayerSetting>();
#else
            var list = new List<S1AvatarFramework.AvatarSettings.LayerSetting>();
#endif

            if (layers != null)
            {
                foreach (var layer in layers)
                {
                    if (!string.IsNullOrWhiteSpace(layer.LayerPath))
                    {
                        list.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
                        {
                            layerPath = layer.LayerPath,
                            layerTint = layer.LayerTint
                        });
                    }
                }
            }

            S1AvatarSettings.FaceLayerSettings = list;
        }

        /// <summary>
        /// Sets body layer settings from a list.
        /// </summary>
        public void SetBodyLayers(List<LayerSetting> layers)
        {
            if (S1AvatarSettings == null)
                return;

#if (IL2CPPMELON)
            var list = new Il2CppCollectionsGeneric.List<S1AvatarFramework.AvatarSettings.LayerSetting>();
#else
            var list = new List<S1AvatarFramework.AvatarSettings.LayerSetting>();
#endif

            if (layers != null)
            {
                foreach (var layer in layers)
                {
                    if (!string.IsNullOrWhiteSpace(layer.LayerPath))
                    {
                        list.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
                        {
                            layerPath = layer.LayerPath,
                            layerTint = layer.LayerTint
                        });
                    }
                }
            }

            S1AvatarSettings.BodyLayerSettings = list;
        }

        /// <summary>
        /// Sets accessory settings from a list.
        /// </summary>
        public void SetAccessories(List<AccessorySetting> accessories)
        {
            if (S1AvatarSettings == null)
                return;

#if (IL2CPPMELON)
            var list = new Il2CppCollectionsGeneric.List<S1AvatarFramework.AvatarSettings.AccessorySetting>();
#else
            var list = new List<S1AvatarFramework.AvatarSettings.AccessorySetting>();
#endif

            if (accessories != null)
            {
                foreach (var accessory in accessories)
                {
                    if (!string.IsNullOrWhiteSpace(accessory.Path))
                    {
                        list.Add(new S1AvatarFramework.AvatarSettings.AccessorySetting
                        {
                            path = accessory.Path,
                            color = accessory.Color
                        });
                    }
                }
            }

            S1AvatarSettings.AccessorySettings = list;
        }

        /// <summary>
        /// Gets all face layer settings.
        /// </summary>
        public List<LayerSetting> GetFaceLayers()
        {
            var result = new List<LayerSetting>();
            if (S1AvatarSettings?.FaceLayerSettings == null)
                return result;

            for (int i = 0; i < S1AvatarSettings.FaceLayerSettings.Count; i++)
            {
                var layer = S1AvatarSettings.FaceLayerSettings[i];
                result.Add(new LayerSetting
                {
                    LayerPath = layer.layerPath ?? string.Empty,
                    LayerTint = layer.layerTint
                });
            }

            return result;
        }

        /// <summary>
        /// Gets all body layer settings.
        /// </summary>
        public List<LayerSetting> GetBodyLayers()
        {
            var result = new List<LayerSetting>();
            if (S1AvatarSettings?.BodyLayerSettings == null)
                return result;

            for (int i = 0; i < S1AvatarSettings.BodyLayerSettings.Count; i++)
            {
                var layer = S1AvatarSettings.BodyLayerSettings[i];
                result.Add(new LayerSetting
                {
                    LayerPath = layer.layerPath ?? string.Empty,
                    LayerTint = layer.layerTint
                });
            }

            return result;
        }

        /// <summary>
        /// Gets all accessory settings.
        /// </summary>
        public List<AccessorySetting> GetAccessories()
        {
            var result = new List<AccessorySetting>();
            if (S1AvatarSettings?.AccessorySettings == null)
                return result;

            for (int i = 0; i < S1AvatarSettings.AccessorySettings.Count; i++)
            {
                var accessory = S1AvatarSettings.AccessorySettings[i];
                result.Add(new AccessorySetting
                {
                    Path = accessory.path ?? string.Empty,
                    Color = accessory.color
                });
            }

            return result;
        }

        /// <summary>
        /// Eye lid configuration structure.
        /// </summary>
        public sealed class EyeLidConfiguration
        {
            /// <summary>
            /// Top lid open value (0.0 to 1.0).
            /// </summary>
            public float TopLidOpen { get; set; }

            /// <summary>
            /// Bottom lid open value (0.0 to 1.0).
            /// </summary>
            public float BottomLidOpen { get; set; }
        }

        /// <summary>
        /// Layer setting structure.
        /// </summary>
        public sealed class LayerSetting
        {
            /// <summary>
            /// Layer path/identifier.
            /// </summary>
            public string LayerPath { get; set; } = string.Empty;

            /// <summary>
            /// Layer tint color.
            /// </summary>
            public Color LayerTint { get; set; }
        }

        /// <summary>
        /// Accessory setting structure.
        /// </summary>
        public sealed class AccessorySetting
        {
            /// <summary>
            /// Accessory path/identifier.
            /// </summary>
            public string Path { get; set; } = string.Empty;

            /// <summary>
            /// Accessory color.
            /// </summary>
            public Color Color { get; set; }
        }
    }
}

