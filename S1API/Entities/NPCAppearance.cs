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
using UnityEngine.UI;

using S1API.Entities.Appearances.AccessoryFields;
using S1API.Entities.Appearances.Base;
using S1API.Entities.Appearances.BodyLayerFields;
using S1API.Entities.Appearances.CustomizationFields;
using S1API.Entities.Appearances.FaceLayerFields;
using S1API.Utils;
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
                else
                    sourceSettings = global::S1API.Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(_runtimeAvatar, "InitialAvatarSettings") as S1AvatarFramework.AvatarSettings;
            }

            if (sourceSettings != null)
                _customAvatarSettings = ScriptableObject.Instantiate(sourceSettings);
            else
            {
                _customAvatarSettings = ScriptableObject.CreateInstance<S1AvatarFramework.AvatarSettings>();
                ApplyDefaultSettings(_customAvatarSettings);
            }

            S1AvatarFramework.AvatarSettings avatarSettings = Resources.Load<S1AvatarFramework.AvatarSettings>($"charactersettings/{NPC.FirstName}");
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
                _hasQueuedMugshots = true;
                if (!_isProcessingMugshots)
                {
                    _isProcessingMugshots = true;
                    MelonCoroutines.Start(ProcessMugshotQueue());
                }
            }
        }

        private static IEnumerator ProcessMugshotQueue()
        {
            var generator = S1AvatarFramework.MugshotGenerator.Instance;
            var mugshotRig = generator != null ? generator.MugshotRig : null;
            var iconGenerator = generator != null ? generator.Generator : null;

            // === Quick physical warmup ===
            // Toggle the rig active/inactive to force the GPU driver and Unity's internal
            // SkinnedMeshRenderer/Animator/material pipeline to initialize. In a modding
            // context (MelonLoader/IL2CPP) we cannot pre-warm shader variants directly,
            // so cycling the GameObject is the only way to trigger lazy initialization.
            // Pixel content is NOT checked here — testing proved DefaultSettings warmup
            // renders produce black frames on cold start even when the rig is fully ready
            // for real NPC captures. Actual content validation happens per-capture in the
            // retry loop below.
            if (mugshotRig != null && generator.DefaultSettings != null)
            {
                int iconLayer = LayerMask.NameToLayer("IconGeneration");
                const int primeCycles = 5;

                for (int warmup = 0; warmup < primeCycles; warmup++)
                {
                    Transform warmupParent = mugshotRig.transform.parent;
                    if (warmupParent != null)
                        warmupParent.gameObject.SetActive(true);
                    mugshotRig.gameObject.SetActive(true);

                    mugshotRig.LoadAvatarSettings(generator.DefaultSettings);
                    SetLayerRecursively(mugshotRig.gameObject, iconLayer);

                    var warmupSMRs = mugshotRig.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var smr in warmupSMRs)
                        smr.updateWhenOffscreen = true;

                    yield return new WaitForEndOfFrame();

                    mugshotRig.LoadAvatarSettings(generator.DefaultSettings);
                    mugshotRig.gameObject.SetActive(false);
                }
            }

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

                // Refresh references in case they became stale
                generator = S1AvatarFramework.MugshotGenerator.Instance;
                mugshotRig = generator != null ? generator.MugshotRig : null;
                iconGenerator = generator != null ? generator.Generator : null;
                if (mugshotRig == null)
                {
                    lock (_mugshotQueueLock)
                        _mugshotQueue.Enqueue(next);
                    yield return null;
                    continue;
                }

                S1AvatarFramework.Avatar previousAvatar = next.NPC.S1NPC.Avatar;
                next.NPC.S1NPC.Avatar = mugshotRig;

                // Use a per-capture clone so subsequent appearance edits don't mutate the in-flight mugshot
                var mugshotSettings = ScriptableObject.Instantiate(next._customAvatarSettings);
                mugshotSettings.Height = 1f;

                // === Content-validated capture with retry ===
                // On cold start the rig's renderers may not be ready, producing completely
                // black textures (brightness 0.0) for 20+ consecutive frames before content
                // appears. This was observed via MelonLoader logs across multiple hardware
                // configs. The high retry count (30) is a safety cap for the worst case;
                // on warm loads (quit-to-menu → reload) content appears on attempt 0.
                // Do NOT reduce this — lower values will break cold start on some machines.
                const int maxRetries = 30;
                const float contentBrightnessFloor = 0.01f;
                Texture2D generatedMugshot = null;
                bool hasContent = false;

                for (int attempt = 0; attempt <= maxRetries; attempt++)
                {
                    Transform mugshotParent = mugshotRig.transform.parent;
                    if (mugshotParent != null)
                        mugshotParent.gameObject.SetActive(true);
                    mugshotRig.gameObject.SetActive(true);

                    // Disable distance culling so the mugshot rig never hides while the player camera is far away
                    bool previousAllowCulling = mugshotRig.Animation != null && mugshotRig.Animation.AllowCulling;
                    if (mugshotRig.Animation != null)
                        mugshotRig.Animation.AllowCulling = false;
                    mugshotRig.SetVisible(true);
                    mugshotRig.Impostor.DisableImpostor();

                    mugshotRig.LoadAvatarSettings(mugshotSettings);
                    SetLayerRecursively(mugshotRig.gameObject, LayerMask.NameToLayer("IconGeneration"));

                    var skinnedMeshRenderers = mugshotRig.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var smr in skinnedMeshRenderers)
                        smr.updateWhenOffscreen = true;

                    // Wait a full frame so the Avatar's internal Update/LateUpdate cycle
                    // can apply skin color and other material properties from the settings.
                    yield return null;
                    yield return new WaitForEndOfFrame();

                    generatedMugshot = null;
                    try
                    {
                        generatedMugshot = iconGenerator.GetTexture(mugshotRig.transform);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Direct GetTexture failed: {ex.Message}");
                    }

                    // Check if capture has actual content (not black/empty)
                    hasContent = false;
                    if (generatedMugshot != null && generatedMugshot.width > 0 && generatedMugshot.height > 0)
                    {
                        int cx = generatedMugshot.width / 2;
                        int cy = generatedMugshot.height / 2;
                        Color centerPx = generatedMugshot.GetPixel(cx, cy);
                        Color topPx = generatedMugshot.GetPixel(cx, (int)(generatedMugshot.height * 0.85f));
                        Color botPx = generatedMugshot.GetPixel(cx, (int)(generatedMugshot.height * 0.15f));
                        Color leftPx = generatedMugshot.GetPixel((int)(generatedMugshot.width * 0.25f), cy);
                        Color rightPx = generatedMugshot.GetPixel((int)(generatedMugshot.width * 0.75f), cy);

                        float maxBrightness = 0f;
                        Color[] samples = { centerPx, topPx, botPx, leftPx, rightPx };
                        foreach (var s in samples)
                        {
                            float b = s.r + s.g + s.b;
                            if (b > maxBrightness) maxBrightness = b;
                        }
                        hasContent = maxBrightness > contentBrightnessFloor;
                    }

                    if (hasContent)
                        break;

                    // No content — deactivate and retry
                    if (generator.DefaultSettings != null)
                        mugshotRig.LoadAvatarSettings(generator.DefaultSettings);
                    if (mugshotRig.Animation != null)
                        mugshotRig.Animation.AllowCulling = previousAllowCulling;
                    mugshotRig.gameObject.SetActive(false);

                    if (attempt == maxRetries)
                        _logger.Warning($"[Mugshot] {next.NPC.FirstName}: no content after {maxRetries + 1} attempts, using last capture");
                }

                if (generatedMugshot != null)
                {
                    try
                    {
                        generatedMugshot.Apply();
                        Rect cropRect = new Rect(0, 0, generatedMugshot.width, generatedMugshot.height);
                        Sprite iconSprite = Sprite.Create(generatedMugshot, cropRect, Vector2.zero);
                        next.NPC.Icon = iconSprite;
                        next.NPC.RefreshMessagingIcons();

                        // Update any map POI icons that reference this NPC
                        UpdatePoiIcons(next.NPC.S1NPC, iconSprite);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to finalize mugshot: {ex.Message}");
                    }
                }

                // Restore avatar reference
                next.NPC.S1NPC.Avatar = previousAvatar ?? next._runtimeAvatar;
                next.ApplyToAvatar(next._runtimeAvatar);

                // Reset rig and deactivate
                bool finalAllowCulling = mugshotRig.Animation != null && mugshotRig.Animation.AllowCulling;
                if (generator.DefaultSettings != null)
                    mugshotRig.LoadAvatarSettings(generator.DefaultSettings);
                if (mugshotRig.Animation != null)
                    mugshotRig.Animation.AllowCulling = finalAllowCulling;
                mugshotRig.gameObject.SetActive(false);

                // Small delay between jobs to let the mugshot rig fully reset
                yield return new WaitForSeconds(0.1f);
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
        private static bool _hasQueuedMugshots = false;

        /// <summary>
        /// INTERNAL: Resets mugshot queue state on scene change so warmup runs fresh on reload.
        /// Called from SceneStateCleaner.
        /// </summary>
        internal static void ResetMugshotState()
        {
            lock (_mugshotQueueLock)
            {
                _mugshotQueue.Clear();
                _isProcessingMugshots = false;
                _hasQueuedMugshots = false;
            }
        }

        /// <summary>
        /// Returns true when all queued mugshots have been processed.
        /// Returns false if no mugshots have ever been queued (generation hasn't started).
        /// </summary>
        internal static bool MugshotsProcessingComplete
        {
            get
            {
                lock (_mugshotQueueLock)
                {
                    // If no mugshots have been queued yet, generation hasn't started
                    if (!_hasQueuedMugshots)
                        return false;
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



