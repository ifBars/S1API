#if (IL2CPPMELON)
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1Map = Il2CppScheduleOne.Map;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1Map = ScheduleOne.Map;
using S1NPCs = ScheduleOne.NPCs;
#endif

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

using S1API.Entities.Appearances.AccessoryFields;
using S1API.Entities.Appearances.Base;
using S1API.Entities.Appearances.BodyLayerFields;
using S1API.Entities.Appearances.CustomizationFields;
using S1API.Entities.Appearances.FaceLayerFields;
using S1API.Internal.Utils;
using S1API.Logging;
using MelonLoader;

namespace S1API.Entities
{
    /// <summary>
    /// Modder-facing appearance customization system for NPCs. Provides builders to configure visual appearance including
    /// physical features, clothing, and accessories. Appearance configuration is done in <see cref="NPC.OnCreated"/>.
    /// </summary>
    /// <remarks>
    /// Use the builder pattern to configure customization fields, face layers, body layers, and accessory layers.
    /// Always call <see cref="Build"/> at the end to generate the mugshot and apply the appearance to the avatar.
    /// </remarks>
    public class NPCAppearance
    {
        private static readonly Log _logger = new Log("NPCAppearance");

        #region Internal Members

        /// <summary>
        /// INTERNAL: Reference to the NPC on API side
        /// </summary>
        internal readonly NPC NPC;

        /// <summary>
        /// INTERNAL: Constructor used for assigning the NPC instance.
        /// </summary>
        /// <param name="npc"></param>
        internal NPCAppearance(NPC npc, S1AvatarFramework.Avatar runtimeAvatar)
        {
            NPC = npc;
            _runtimeAvatar = runtimeAvatar;

            S1AvatarFramework.AvatarSettings sourceSettings = null;

            if (_runtimeAvatar != null)
            {
                if (_runtimeAvatar.CurrentSettings != null)
                    sourceSettings = _runtimeAvatar.CurrentSettings;
                else if (_runtimeAvatar.InitialAvatarSettings != null)
                    sourceSettings = _runtimeAvatar.InitialAvatarSettings;
            }

            if (sourceSettings != null)
                _customAvatarSettings = ScriptableObject.Instantiate(sourceSettings);
            else
            {
                _customAvatarSettings = ScriptableObject.CreateInstance<S1AvatarFramework.AvatarSettings>();
                ApplyDefaultSettings(_customAvatarSettings);
            }

            S1AvatarFramework.AvatarSettings avatarSettings = Resources.Load<S1AvatarFramework.AvatarSettings>($"charactersettings/{NPC.S1NPC.FirstName}");
            if (avatarSettings != null)
                _customAvatarSettings = ScriptableObject.Instantiate(avatarSettings);

            ApplyToAvatar(_runtimeAvatar);
        }

        /// <summary>
        /// Generate the Mugshot for the <see cref="NPC"/> instance
        /// </summary>
        internal void GenerateMugshot()
        {
            // Enqueue serialized mugshot generation to avoid shared rig race conditions
            var generator = S1AvatarFramework.MugshotGenerator.Instance;
            if (generator == null || generator.MugshotRig == null)
                return;

            lock (_mugshotQueueLock)
            {
                _mugshotQueue.Enqueue(this);
                if (!_isProcessingMugshots)
                {
                    _isProcessingMugshots = true;
                    MelonCoroutines.Start(ProcessMugshotQueue());
                }
            }
        }

        private static IEnumerator ProcessMugshotQueue()
        {
            while (true)
            {
                NPCAppearance next = null;
                lock (_mugshotQueueLock)
                {
                    if (_mugshotQueue.Count > 0)
                        next = _mugshotQueue.Dequeue();
                    else
                    {
                        _isProcessingMugshots = false;
                        yield break;
                    }
                }

                if (next == null)
                {
                    yield return null;
                    continue;
                }

                var generator = S1AvatarFramework.MugshotGenerator.Instance;
                var mugshotRig = generator != null ? generator.MugshotRig : null;
                var iconGenerator = generator != null ? generator.Generator : null;
                if (mugshotRig == null)
                {
                    // Nothing we can do this frame; try again next frame
                    // Re-enqueue to avoid dropping the request if generator is momentarily unavailable
                    lock (_mugshotQueueLock)
                        _mugshotQueue.Enqueue(next);
                    yield return null;
                    continue;
                }

                // Phase 1: setup without yielding
                S1AvatarFramework.Avatar previousAvatar = next.NPC.S1NPC.Avatar;
                next.NPC.S1NPC.Avatar = mugshotRig;
                Transform mugshotParent = mugshotRig.transform.parent;
                if (mugshotParent != null)
                    mugshotParent.gameObject.SetActive(true);

                // Activate the rig (same as game's GenerateMugshot)
                mugshotRig.gameObject.SetActive(true);

                // Use a per-capture clone so subsequent appearance edits don't mutate the in-flight mugshot
                var mugshotSettings = ScriptableObject.Instantiate(next._customAvatarSettings);
                mugshotSettings.Height = 1f;
                next.NPC.S1NPC.Avatar.LoadAvatarSettings(mugshotSettings);

                // Set layer for icon generation (same as game's GenerateMugshot)
                SetLayerRecursively(mugshotRig.gameObject, LayerMask.NameToLayer("IconGeneration"));

                // Enable updateWhenOffscreen for proper bounds calculation (same as game's GenerateMugshot)
                var skinnedMeshRenderers = mugshotRig.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var smr in skinnedMeshRenderers)
                {
                    smr.updateWhenOffscreen = true;
                }

                // Capture current lighting state so the ambient flip from the generator doesn't leak
                var previousAmbientMode = RenderSettings.ambientMode;
                var previousAmbientLight = RenderSettings.ambientLight;
                var previousAmbientIntensity = RenderSettings.ambientIntensity;
                bool? previousModifyLighting = iconGenerator != null ? iconGenerator.ModifyLighting : null;
                if (iconGenerator != null)
                    iconGenerator.ModifyLighting = true;

                // Give the rig a frame to update meshes/bounds with the new settings before capture
                yield return new WaitForEndOfFrame();

                bool completed = false;
                // Trigger capture (callback will flip flag)
                next.NPC.S1NPC.Avatar.GetMugshot((Action<Texture2D>)(generatedMugshot =>
                {
                    try
                    {
                        generatedMugshot.Apply();

                        // Apply a modest crop to handle accessories (like hats) that extend bounds
                        // This ensures faces are consistently sized regardless of head accessories
                        float cropScale = 0.92f; // Take 92% of the image
                        int cropWidth = Mathf.RoundToInt(generatedMugshot.width * cropScale);
                        int cropHeight = Mathf.RoundToInt(generatedMugshot.height * cropScale);
                        int cropX = (generatedMugshot.width - cropWidth) / 2; // Center horizontally
                        int cropY = Mathf.RoundToInt(generatedMugshot.height * 0.04f); // Slight offset up for face focus

                        // Clamp to valid bounds
                        cropX = Mathf.Clamp(cropX, 0, generatedMugshot.width - cropWidth);
                        cropY = Mathf.Clamp(cropY, 0, generatedMugshot.height - cropHeight);

                        Rect cropRect = new Rect(cropX, cropY, cropWidth, cropHeight);
                        Sprite iconSprite = Sprite.Create(generatedMugshot, cropRect, new Vector2(0.5f, 0.5f));
                        next.NPC.Icon = iconSprite;
                        next.NPC.RefreshMessagingIcons();

                        // Update any map POI icons that reference this NPC
                        UpdatePoiIcons(next.NPC.S1NPC, iconSprite);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to finalize mugshot: {ex.Message}");
                    }
                    finally
                    {
                        completed = true;
                    }
                }));

                // Phase 2: wait for completion
                while (!completed)
                    yield return null;

                // Restore scene/global state
                if (iconGenerator != null && previousModifyLighting.HasValue)
                    iconGenerator.ModifyLighting = previousModifyLighting.Value;
                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbientLight;
                RenderSettings.ambientIntensity = previousAmbientIntensity;

                // Phase 3: restore without yielding
                next.NPC.S1NPC.Avatar = previousAvatar ?? next._runtimeAvatar;
                next.ApplyToAvatar(next._runtimeAvatar);

                // Restore rig to default state (same as game's GenerateMugshot)
                if (generator.DefaultSettings != null)
                    mugshotRig.LoadAvatarSettings(generator.DefaultSettings);
                mugshotRig.gameObject.SetActive(false);

                // Small delay between jobs to let the mugshot rig fully reset
                yield return new WaitForSeconds(0.05f);
            }
        }

        /// <summary>
        /// INTERNAL: Applies the currently configured avatar settings to a runtime avatar instance.
        /// </summary>
        /// <param name="avatar">The avatar to apply settings to.</param>
        internal void ApplyToAvatar(S1AvatarFramework.Avatar avatar)
        {
            if (avatar == null)
                return;

            avatar.LoadAvatarSettings(_customAvatarSettings);
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Sets an appearance field within <see cref="S1AvatarFramework.AvatarSettings"/>
        /// </summary>
        /// <typeparam name="T">The appearance type</typeparam>
        /// <param name="appearanceValue">The value to set</param>
        public NPCAppearance Set<T>(object appearanceValue) where T : BaseAppearance
        {
            if (_setters.TryGetValue(typeof(T), out var setter))
            {
                try
                {
                    setter(this, appearanceValue);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to set {typeof(T).Name}: {ex.Message}");
                }
            }
            else
                _logger.Error($"No setter registered for appearance type {typeof(T).Name}");

            return this;
        }

        /// <summary>
        /// Adds a Face Layer within the <see cref="S1AvatarFramework.AvatarSettings"/>
        /// </summary>
        /// <param name="path">The asset path</param>
        /// <param name="hexColor">The color in Hexadecimals</param>
        /// <typeparam name="T"></typeparam>
        public NPCAppearance WithFaceLayer<T>(string path, uint hexColor) where T : BaseFaceAppearance =>
            WithFaceLayer<T>(path, hexColor.ToColor());

        /// <summary>
        /// Adds a Face Layer within the <see cref="S1AvatarFramework.AvatarSettings"/>
        /// </summary>
        /// <param name="path">The asset path</param>
        /// <param name="color">The color instance</param>
        /// <typeparam name="T"></typeparam>
        public NPCAppearance WithFaceLayer<T>(string path, Color color) where T : BaseFaceAppearance
        {
            if (_customAvatarSettings.FaceLayerSettings.Count > MaxFaceLayers)
                return this;

            _customAvatarSettings.FaceLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
            {
                layerPath = path,
                layerTint =  color
            });

            return this;
        }

        /// <summary>
        /// Adds a Body Layer within the <see cref="S1AvatarFramework.AvatarSettings"/>
        /// </summary>
        /// <param name="path">The asset path</param>
        /// <param name="hexColor">The color in Hexadecimals</param>
        /// <typeparam name="T"></typeparam>
        public NPCAppearance WithBodyLayer<T>(string path, uint hexColor) where T : BaseBodyAppearance =>
            WithBodyLayer<T>(path, hexColor.ToColor());

        /// <summary>
        /// Adds a Body Layer within the <see cref="S1AvatarFramework.AvatarSettings"/>
        /// </summary>
        /// <param name="path">The asset path</param>
        /// <param name="color">The color instance</param>
        /// <typeparam name="T"></typeparam>
        public NPCAppearance WithBodyLayer<T>(string path, Color color) where T : BaseBodyAppearance
        {
            if (_customAvatarSettings.BodyLayerSettings.Count > MaxBodyLayers)
                return this;

            _customAvatarSettings.BodyLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
            {
                layerPath = path,
                layerTint =  color
            });

            return this;
        }

        /// <summary>
        /// Adds a Accessory Layer within the <see cref="S1AvatarFramework.AvatarSettings"/>
        /// </summary>
        /// <param name="path">The asset path</param>
        /// <param name="hexColor">The color in Hexadecimals</param>
        /// <typeparam name="T"></typeparam>
        public NPCAppearance WithAccessoryLayer<T>(string path, uint hexColor) where T : BaseAccessoryAppearance =>
            WithAccessoryLayer<T>(path, hexColor.ToColor());

        /// <summary>
        /// Adds a Accessory Layer within the <see cref="S1AvatarFramework.AvatarSettings"/>
        /// </summary>
        /// <param name="path">The asset path</param>
        /// <param name="color">The color instance</param>
        /// <typeparam name="T"></typeparam>
        public NPCAppearance WithAccessoryLayer<T>(string path, Color color) where T : BaseAccessoryAppearance
        {
            if (_customAvatarSettings.AccessorySettings.Count > MaxAccessoryLayers)
                return this;

            _customAvatarSettings.AccessorySettings.Add(new S1AvatarFramework.AvatarSettings.AccessorySetting
            {
                path = path,
                color =  color
            });

            return this;
        }

        /// <summary>
        /// Finalizes the appearance by generating the NPC's mugshot.
        /// This can be called after setting all appearance attributes.
        /// </summary>
        /// <returns>The <see cref="NPCAppearance"/> instance with the generated mugshot.</returns>
        public NPCAppearance Build()
        {
            GenerateMugshot();
            return this;
        }

        /// <summary>
        /// Generates a random appearance for the <see cref="NPC"/>
        /// </summary>
        public void GenerateRandomAppearance()
        {
            Color RandomColor() => new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            float RandomRange(float min, float max) => UnityEngine.Random.Range(min, max);
            string RandomFromList(List<string> list) => list[UnityEngine.Random.Range(0, list.Count)];

            #region Customization Fields

            // Customization fields
            Set<EyeBallTint>(RandomColor());
            Set<EyebrowRestingAngle>(RandomRange(0f, 1f));
            Set<EyebrowRestingHeight>(RandomRange(0f, 1f));
            Set<EyebrowScale>(RandomRange(0f, 1f));
            Set<EyebrowThickness>(RandomRange(0f, 1f));

            float topLid = RandomRange(0f, 1f);
            float bottomLid = RandomRange(0f, 1f);
            Set<EyeLidRestingStateLeft>((topLid, bottomLid));
            Set<EyeLidRestingStateRight>((topLid, bottomLid));

            // Hair
            var hairColor = RandomColor();
            Set<HairColor>(hairColor);

            var hairStyles = BaseAppearance.GetConstPaths<HairStyle>();
            if (hairStyles.Count > 0)
                Set<HairStyle>(RandomFromList(hairStyles));

            Set<Gender>(RandomRange(0f, 1f));
            Set<Height>(RandomRange(0.8f, 1.2f));
            Set<PupilDilation>(RandomRange(0f, 1f));
            Set<SkinColor>(RandomColor());
            Set<Weight>(RandomRange(0f, 1f));

            #endregion

            #region Face Layers

            var faceColor = RandomColor();

            // Required: Eyes and Face
            var eyes = BaseFaceAppearance.GetConstPaths<Eyes>();
            if (eyes.Count > 0)
                WithFaceLayer<Eyes>(RandomFromList(eyes), faceColor);

            var faces = BaseFaceAppearance.GetConstPaths<Face>();
            if (faces.Count > 0)
                WithFaceLayer<Face>(RandomFromList(faces), faceColor);

            // Optional: FacialHair (50% chance)
            if (UnityEngine.Random.value < 0.5f)
            {
                var facialHair = BaseFaceAppearance.GetConstPaths<FacialHair>();
                if (facialHair.Count > 0)
                    WithFaceLayer<FacialHair>(RandomFromList(facialHair), hairColor);
            }

            #endregion

            #region Body Layers

            var bodyTypes = new (Type type, Action<string, Color> apply)[]
            {
                (typeof(Shirts), (path, color) => WithBodyLayer<Shirts>(path, color)),
                (typeof(Pants), (path, color) => WithBodyLayer<Pants>(path, color))
            };
            foreach (var (type, apply) in bodyTypes.OrderBy(_ => Guid.NewGuid()).Take(UnityEngine.Random.Range(1, 3)))
            {
                MethodInfo method = AccessTools.Method(typeof(BaseBodyAppearance), "GetConstPaths").MakeGenericMethod(type);
                List<string> paths = (List<string>)method.Invoke(null, null);
                if (paths?.Count > 0)
                    apply(RandomFromList(paths), RandomColor());
            }

            #endregion

            #region Accessory Layers

            var accessoryLayers = new (Type type, Action<string, Color> apply)[]
            {
                (typeof(Bottom), (path, color) => WithAccessoryLayer<Bottom>(path, color)),
                (typeof(Chest), (path, color) => WithAccessoryLayer<Chest>(path, color)),
                (typeof(Feet), (path, color) => WithAccessoryLayer<Feet>(path, color)),
                (typeof(Hands), (path, color) => WithAccessoryLayer<Hands>(path, color)),
                (typeof(Head), (path, color) => WithAccessoryLayer<Head>(path, color)),
                (typeof(Neck), (path, color) => WithAccessoryLayer<Neck>(path, color)),
                (typeof(Waist), (path, color) => WithAccessoryLayer<Waist>(path, color))
            };
            foreach (var (type, apply) in accessoryLayers.OrderBy(_ => Guid.NewGuid()).Take(UnityEngine.Random.Range(2, 6)))
            {
                MethodInfo method = AccessTools.Method(typeof(BaseAccessoryAppearance), "GetConstPaths").MakeGenericMethod(type);
                List<string> paths = (List<string>)method.Invoke(null, null);
                if (paths?.Count > 0)
                    apply(RandomFromList(paths), RandomColor());
            }

            #endregion
        }

        #endregion

        #region Private Members

        /// <summary>
        /// INTERNAL: Set's default values to <see cref="S1AvatarFramework.AvatarSettings"/>
        /// </summary>
        /// <remarks>This function is grabbed from the client code</remarks>
        /// <param name="avatarSettings">The AvatarSettings to assign defaults to</param>
        private static void ApplyDefaultSettings(S1AvatarFramework.AvatarSettings avatarSettings)
        {
            avatarSettings.SkinColor = new Color32(150, 120, 95, byte.MaxValue);
            avatarSettings.Height = 0.98f;
            avatarSettings.Gender = 0.0f;
            avatarSettings.Weight = 0.4f;
            avatarSettings.EyebrowScale = 1f;
            avatarSettings.EyebrowThickness = 1f;
            avatarSettings.EyebrowRestingHeight = 0.0f;
            avatarSettings.EyebrowRestingAngle = 0.0f;
            avatarSettings.LeftEyeLidColor = new Color32(150, 120, 95, byte.MaxValue);
            avatarSettings.RightEyeLidColor = new Color32(150, 120, 95, byte.MaxValue);
            avatarSettings.LeftEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
            {
                bottomLidOpen = 0.5f,
                topLidOpen = 0.5f
            };
            avatarSettings.RightEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
            {
                bottomLidOpen = 0.5f,
                topLidOpen = 0.5f
            };
            avatarSettings.EyeballMaterialIdentifier = "Default";
            avatarSettings.EyeBallTint = Color.white;
            avatarSettings.PupilDilation = 1f;
            avatarSettings.HairPath = string.Empty;
            avatarSettings.HairColor = Color.black;
        }

        private S1AvatarFramework.Avatar _runtimeAvatar;

        /// <summary>
        /// INTERNAL: The custom <see cref="S1AvatarFramework.AvatarSettings"/> instance used for modders
        /// </summary>
        private S1AvatarFramework.AvatarSettings _customAvatarSettings;

        /// <summary>
        /// INTERNAL: Setters for each individual property (blame IL2CPP)
        /// </summary>
        private static readonly Dictionary<Type, Action<NPCAppearance, object>> _setters = new Dictionary<Type, Action<NPCAppearance, object>>
        {
            [typeof(HairStyle)] = (self, value) => self._customAvatarSettings.HairPath = value as string ?? string.Empty,
            [typeof(HairColor)] = (self, value) =>
            {
                self._customAvatarSettings.HairColor = value switch
                {
                    uint hex => hex.ToColor(),
                    Color color => color,
                    _ => self._customAvatarSettings.HairColor
                };
            },
            [typeof(SkinColor)] = (self, value) =>
            {
                self._customAvatarSettings.SkinColor = value switch
                {
                    uint hex => hex.ToColor(),
                    Color color => color,
                    _ => self._customAvatarSettings.SkinColor
                };
            },
            [typeof(EyeBallTint)] = (self, value) =>
            {
                self._customAvatarSettings.EyeBallTint = value switch
                {
                    uint hex => hex.ToColor(),
                    Color color => color,
                    _ => self._customAvatarSettings.EyeBallTint
                };
            },
            [typeof(Gender)] = (self, value) => self._customAvatarSettings.Gender = Convert.ToSingle(value),
            [typeof(Height)] = (self, value) => self._customAvatarSettings.Height = Convert.ToSingle(value),
            [typeof(Weight)] = (self, value) => self._customAvatarSettings.Weight = Convert.ToSingle(value),
            [typeof(PupilDilation)] = (self, value) => self._customAvatarSettings.PupilDilation = Convert.ToSingle(value),
            [typeof(EyebrowRestingAngle)] = (self, value) => self._customAvatarSettings.EyebrowRestingAngle = Convert.ToSingle(value),
            [typeof(EyebrowRestingHeight)] = (self, value) => self._customAvatarSettings.EyebrowRestingHeight = Convert.ToSingle(value),
            [typeof(EyebrowScale)] = (self, value) => self._customAvatarSettings.EyebrowScale = Convert.ToSingle(value),
            [typeof(EyebrowThickness)] = (self, value) => self._customAvatarSettings.EyebrowThickness = Convert.ToSingle(value),
            [typeof(EyeLidRestingStateLeft)] = (self, value) =>
            {
                var items = value.GetValueTupleItems();
                self._customAvatarSettings.LeftEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
                {
                    topLidOpen = Convert.ToSingle(items![0]),
                    bottomLidOpen = Convert.ToSingle(items![1])
                };
            },
            [typeof(EyeLidRestingStateRight)] = (self, value) =>
            {
                var items = value.GetValueTupleItems();
                self._customAvatarSettings.RightEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
                {
                    topLidOpen = Convert.ToSingle(items![0]),
                    bottomLidOpen = Convert.ToSingle(items![1])
                };
            }
        };

        /// <summary>
        /// INTERNAL: The max amount of layers for the Face
        /// </summary>
        private const int MaxFaceLayers = 6;

        /// <summary>
        /// INTERNAL: The max amount of layers for the Body
        /// </summary>
        private const int MaxBodyLayers = 6;

        /// <summary>
        /// INTERNAL: The max amount of layers for Accessories
        /// </summary>
        private const int MaxAccessoryLayers = 9;

        #endregion

        #region Static Mugshot Queue

        private static readonly object _mugshotQueueLock = new object();
        private static readonly Queue<NPCAppearance> _mugshotQueue = new Queue<NPCAppearance>();
        private static bool _isProcessingMugshots = false;

        /// <summary>
        /// Returns true when all queued mugshots have been processed.
        /// </summary>
        internal static bool MugshotsProcessingComplete
        {
            get
            {
                lock (_mugshotQueueLock)
                {
                    return !_isProcessingMugshots && _mugshotQueue.Count == 0;
                }
            }
        }

        /// <summary>
        /// Sets the layer of a GameObject and all its children recursively.
        /// </summary>
        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
#if (IL2CPPMELON || IL2CPPBEPINEX)
            // Il2Cpp: foreach iteration returns Il2CppSystem.Object, use index-based access
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
            }
#else
            // Mono: foreach works directly with Transform
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
#endif
        }

        /// <summary>
        /// Updates the icon on any map POI components that reference the given NPC.
        /// </summary>
        private static void UpdatePoiIcons(S1NPCs.NPC npc, Sprite iconSprite)
        {
            if (npc == null || iconSprite == null)
                return;

            try
            {
                // Find all NPCPoI components in the scene that reference this NPC
                var allPoIs = UnityEngine.Object.FindObjectsOfType<S1Map.NPCPoI>();
                foreach (var poi in allPoIs)
                {
                    if (poi.NPC == npc && poi.IconContainer != null)
                    {
                        var iconTransform = poi.IconContainer.Find("Outline/Icon");
                        if (iconTransform != null)
                        {
                            var image = iconTransform.GetComponent<Image>();
                            if (image != null)
                            {
                                image.sprite = iconSprite;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to update POI icons: {ex.Message}");
            }
        }

        #endregion

    }
}



