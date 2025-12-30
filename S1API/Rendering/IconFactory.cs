#if (IL2CPPMELON)
using S1DevUtils = Il2CppScheduleOne.DevUtilities;
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using Il2CppScheduleOne.AvatarFramework.Customization;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1DevUtils = ScheduleOne.DevUtilities;
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using ScheduleOne.AvatarFramework.Customization;
#endif

using S1API.Logging;
using S1API.Internal.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace S1API.Rendering
{
    /// <summary>
    /// Factory for generating item icons using the game's IconGenerator and MugshotGenerator.
    /// <para>
    /// <b>Item Icon Generation (Experimental):</b> Direct item icon generation using IconGenerator is experimental.
    /// Use <see cref="GenerateIcon"/> for static mesh item models.
    /// </para>
    /// <para>
    /// <b>Accessory Icon Generation (Confirmed Working):</b> Accessory icon generation using MugshotGenerator
    /// is confirmed to work. Use <see cref="GenerateAccessoryIcon"/> or <see cref="GenerateAccessoryIconSprite"/>
    /// for clothing accessories (hats, glasses, etc.).
    /// </para>
    /// </summary>
    public static class IconFactory
    {
        private static readonly Log Logger = new Log("IconFactory");

        /// <summary>
        /// INTERNAL: Reference to the game's IconGenerator instance.
        /// </summary>
        internal static S1DevUtils.IconGenerator S1IconGenerator =>
            S1DevUtils.IconGenerator.Instance;

        /// <summary>
        /// INTERNAL: Reference to the game's MugshotGenerator instance.
        /// </summary>
        internal static S1AvatarFramework.MugshotGenerator S1MugshotGenerator =>
            S1AvatarFramework.MugshotGenerator.Instance;

        /// <summary>
        /// Generates a preview texture for the specified model.
        /// </summary>
        /// <param name="model">The model to generate an icon for.</param>
        /// <param name="size">The size of the square icon (default 512).</param>
        /// <param name="bakeSkinnedMeshes">If true, bakes SkinnedMeshRenderers to static MeshRenderers to ensure correct bounds (default true).</param>
        /// <returns>A Texture2D containing the icon, or null if generation failed.</returns>
        public static Texture2D? GenerateIcon(Transform model, int size = 512, bool bakeSkinnedMeshes = true)
        {
            if (S1IconGenerator == null)
            {
                Logger.Error("IconGenerator not found in scene. Cannot generate icon.");
                return null;
            }

            if (model == null)
            {
                Logger.Error("Model transform is null when generating icon.");
                return null;
            }

            try
            {
                // Store original transform state for restoration after generation
                Transform? originalParent = model.parent;
                Vector3 originalPos = model.localPosition;
                Quaternion originalRot = model.localRotation;
                Vector3 originalScale = model.localScale;
                bool wasActive = model.gameObject.activeSelf;

                // Parent to ItemContainer first (like the game expects) then position
                model.SetParent(S1IconGenerator.ItemContainer, false);
                model.localPosition = Vector3.zero;
                model.localRotation = Quaternion.identity;
                model.localScale = Vector3.one;
                
                // Now activate and set layers (after parenting)
                model.gameObject.SetActive(true);
                
                int iconLayer = LayerMask.NameToLayer("IconGeneration");
                if (iconLayer != -1)
                {
                    // Set layers recursively on ItemContainer to match game's approach
                    S1DevUtils.LayerUtility.SetLayerRecursively(S1IconGenerator.ItemContainer.gameObject, iconLayer);
                }

                Logger.Msg($"Icon generation for '{model.name}': world pos={model.position}, layer={model.gameObject.layer} ({LayerMask.LayerToName(model.gameObject.layer)}), container pos={S1IconGenerator.ItemContainer.position}");

                // Bake SkinnedMeshRenderers if requested to fix bounds calculation issues
                System.Collections.Generic.List<SkinnedMeshRendererState>? bakedRenderers = null;
                if (bakeSkinnedMeshes)
                {
                    bakedRenderers = BakeSkinnedMeshRenderers(model.gameObject);
                    Logger.Msg($"Baked {bakedRenderers.Count} SkinnedMeshRenderer(s)");
                }

                // Center the model in the container to ensure it's in view of the camera
                CenterModelInContainer(model, S1IconGenerator.ItemContainer);

                // Temporarily override IconGenerator state
                int originalSize = S1IconGenerator.IconSize;
                bool originalModifyLighting = S1IconGenerator.ModifyLighting;

                S1IconGenerator.IconSize = size;
                S1IconGenerator.ModifyLighting = true;

                Texture2D texture = S1IconGenerator.GetTexture(model);
                Logger.Msg($"Generated texture: {(texture != null ? $"{texture.width}x{texture.height}" : "null")}");

                // Restore original state
                S1IconGenerator.IconSize = originalSize;
                S1IconGenerator.ModifyLighting = originalModifyLighting;

                // Restore SkinnedMeshRenderers if they were baked
                if (bakedRenderers != null)
                {
                    RestoreSkinnedMeshRenderers(bakedRenderers);
                }
                
                model.SetParent(originalParent, false);
                model.localPosition = originalPos;
                model.localRotation = originalRot;
                model.localScale = originalScale;
                model.gameObject.SetActive(wasActive);

                return texture;
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Failed to generate icon for {model.name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates an icon for a packaging ID and product ID.
        /// </summary>
        /// <param name="packagingID">The ID of the packaging visuals to use.</param>
        /// <param name="productID">The ID of the product to display in the packaging.</param>
        /// <returns>A Texture2D containing the packaging icon, or null if generation failed.</returns>
        public static Texture2D? GeneratePackagingIcon(string packagingID, string productID)
        {
            if (S1IconGenerator == null)
            {
                Logger.Error("IconGenerator not found in scene. Cannot generate icon.");
                return null;
            }

            try
            {
                return S1IconGenerator.GeneratePackagingIcon(packagingID, productID);
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Failed to generate packaging icon for P:{packagingID} Prod:{productID}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates a preview icon as a Sprite for the specified model.
        /// </summary>
        /// <param name="model">The model to generate an icon for.</param>
        /// <param name="size">The size of the square icon (default 512).</param>
        /// <param name="bakeSkinnedMeshes">If true, bakes SkinnedMeshRenderers to static MeshRenderers to ensure correct bounds (default true).</param>
        /// <returns>A Sprite containing the icon, or null if generation failed.</returns>
        public static Sprite? GenerateIconSprite(Transform model, int size = 512, bool bakeSkinnedMeshes = true) =>
            ImageUtils.TextureToSprite(GenerateIcon(model, size, bakeSkinnedMeshes));

        /// <summary>
        /// Generates an icon as a Sprite for a packaging ID and product ID.
        /// </summary>
        /// <param name="packagingID">The ID of the packaging visuals to use.</param>
        /// <param name="productID">The ID of the product to display in the packaging.</param>
        /// <returns>A Sprite containing the packaging icon, or null if generation failed.</returns>
        public static Sprite? GeneratePackagingIconSprite(string packagingID, string productID) =>
            ImageUtils.TextureToSprite(GeneratePackagingIcon(packagingID, productID));

        #region Accessory Icon Generation

        /// <summary>
        /// Generates an icon for an accessory by rendering it on an avatar using MugshotGenerator.
        /// This is an asynchronous operation that will invoke the callback when complete.
        /// </summary>
        /// <param name="accessoryPath">The resource path to the accessory (e.g., from Appearances.AccessoryFields.Head)</param>
        /// <param name="callback">Callback invoked with the generated texture when complete</param>
        /// <param name="accessoryColor">Optional tint color for the accessory (defaults to white)</param>
        /// <param name="size">The size of the square icon (default 512)</param>
        /// <remarks>
        /// This method attempts to use the local player's current avatar settings for a personalized icon.
        /// However, if called during mod initialization (e.g., in OnSceneWasLoaded), the player may not have
        /// spawned yet, causing it to fall back to a generic avatar with neutral face, eyes, and basic clothing.
        /// To use the player's actual appearance, consider deferring icon generation until after player spawn
        /// by subscribing to <see cref="Entities.Player.LocalPlayerSpawned"/>.
        /// </remarks>
        public static void GenerateAccessoryIcon(string accessoryPath, Action<Texture2D> callback, Color? accessoryColor = null, int size = 512)
        {
            if (string.IsNullOrEmpty(accessoryPath))
            {
                Logger.Error("Accessory path is null or empty when generating accessory icon.");
                callback?.Invoke(null);
                return;
            }

            if (callback == null)
            {
                Logger.Error("Callback is null when generating accessory icon.");
                return;
            }

            var request = new AccessoryIconRequest
            {
                AccessoryPath = accessoryPath,
                AccessoryColor = accessoryColor ?? Color.white,
                IconSize = size,
                Callback = callback
            };

            lock (_accessoryIconQueueLock)
            {
                _accessoryIconQueue.Enqueue(request);
                if (!_isProcessingAccessoryIcons)
                {
                    _isProcessingAccessoryIcons = true;
                    MelonCoroutines.Start(ProcessAccessoryIconQueue());
                }
            }
        }

        /// <summary>
        /// Generates an icon for an accessory as a Sprite by rendering it on an avatar using MugshotGenerator.
        /// This is an asynchronous operation that will invoke the callback when complete.
        /// </summary>
        /// <param name="accessoryPath">The resource path to the accessory (e.g., from Appearances.AccessoryFields.Head)</param>
        /// <param name="callback">Callback invoked with the generated sprite when complete</param>
        /// <param name="accessoryColor">Optional tint color for the accessory (defaults to white)</param>
        /// <param name="size">The size of the square icon (default 512)</param>
        public static void GenerateAccessoryIconSprite(string accessoryPath, Action<Sprite> callback, Color? accessoryColor = null, int size = 512)
        {
            GenerateAccessoryIcon(accessoryPath, texture =>
            {
                callback?.Invoke(ImageUtils.TextureToSprite(texture));
            }, accessoryColor, size);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Internal state tracking for SkinnedMeshRenderer baking.
        /// </summary>
        private class SkinnedMeshRendererState
        {
            public SkinnedMeshRenderer SkinnedRenderer { get; set; }
            public MeshFilter? MeshFilter { get; set; }
            public MeshRenderer? MeshRenderer { get; set; }
            public Mesh? BakedMesh { get; set; }

            public SkinnedMeshRendererState(SkinnedMeshRenderer skinnedRenderer)
            {
                SkinnedRenderer = skinnedRenderer;
            }
        }

        /// <summary>
        /// Bakes all SkinnedMeshRenderers in a GameObject to static MeshRenderers.
        /// This fixes bounds calculation issues that cause blank icons.
        /// </summary>
        /// <param name="gameObject">The GameObject to process.</param>
        /// <returns>List of state objects for restoration.</returns>
        private static System.Collections.Generic.List<SkinnedMeshRendererState> BakeSkinnedMeshRenderers(GameObject gameObject)
        {
            var states = new System.Collections.Generic.List<SkinnedMeshRendererState>();
            var skinnedRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var smr in skinnedRenderers)
            {
                var state = new SkinnedMeshRendererState(smr);

                // Create baked mesh
                state.BakedMesh = new Mesh();
                smr.BakeMesh(state.BakedMesh);

                // Add static mesh components
                state.MeshFilter = smr.gameObject.AddComponent<MeshFilter>();
                state.MeshRenderer = smr.gameObject.AddComponent<MeshRenderer>();

                // Apply the baked mesh
                state.MeshFilter.sharedMesh = state.BakedMesh;
                state.MeshRenderer.sharedMaterials = smr.sharedMaterials;

                // Disable the SkinnedMeshRenderer so only the static mesh is rendered
                smr.enabled = false;

                states.Add(state);
            }

            return states;
        }

        /// <summary>
        /// Restores SkinnedMeshRenderers to their original state after baking.
        /// </summary>
        /// <param name="states">List of state objects from BakeSkinnedMeshRenderers.</param>
        private static void RestoreSkinnedMeshRenderers(System.Collections.Generic.List<SkinnedMeshRendererState> states)
        {
            foreach (var state in states)
            {
                // Re-enable the SkinnedMeshRenderer
                if (state.SkinnedRenderer != null)
                {
                    state.SkinnedRenderer.enabled = true;
                }

                // Destroy temporary components
                if (state.MeshRenderer != null)
                {
                    UnityEngine.Object.Destroy(state.MeshRenderer);
                }
                if (state.MeshFilter != null)
                {
                    UnityEngine.Object.Destroy(state.MeshFilter);
                }
                if (state.BakedMesh != null)
                {
                    UnityEngine.Object.Destroy(state.BakedMesh);
                }
            }
        }

        /// <summary>
        /// Centers the model within the container based on its renderer bounds.
        /// This ensures the camera (which looks at the container origin) captures the object
        /// even if the object's pivot is offset (e.g. head accessories).
        /// </summary>
        private static void CenterModelInContainer(Transform model, Transform container)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = new Bounds(model.position, Vector3.zero);
            bool hasBounds = false;

            foreach (var r in renderers)
            {
                if (!r.enabled) continue; // Skip disabled renderers (like baked SkinnedMeshRenderers)
                
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (hasBounds)
            {
                // Calculate how far the center of bounds is from the container pivot
                Vector3 centerOffset = container.position - bounds.center;
                
                // We only want to center, we don't want to mess up scale or anything.
                // Moving the model position shifts the bounds center to the container position.
                model.position += centerOffset;

                Logger.Msg($"Recentered model '{model.name}'. Old Bounds Center: {bounds.center}, New Center: {container.position}, Offset: {centerOffset}");
            }
            else
            {
                Logger.Warning($"Could not calculate bounds for {model.name} (no enabled renderers?). Icon might be empty.");
            }
        }

        #endregion

        #region Accessory Icon Generation - Private Implementation

        /// <summary>
        /// Internal state for accessory icon generation requests.
        /// </summary>
        private class AccessoryIconRequest
        {
            public string AccessoryPath { get; set; }
            public Color AccessoryColor { get; set; }
            public int IconSize { get; set; }
            public Action<Texture2D> Callback { get; set; }
        }

        private static readonly object _accessoryIconQueueLock = new object();
        private static readonly Queue<AccessoryIconRequest> _accessoryIconQueue = new Queue<AccessoryIconRequest>();
        private static bool _isProcessingAccessoryIcons = false;

        /// <summary>
        /// Processes the accessory icon generation queue serially to avoid conflicts with NPC mugshot generation.
        /// </summary>
        private static IEnumerator ProcessAccessoryIconQueue()
        {
            while (true)
            {
                AccessoryIconRequest next = null;
                lock (_accessoryIconQueueLock)
                {
                    if (_accessoryIconQueue.Count > 0)
                        next = _accessoryIconQueue.Dequeue();
                    else
                    {
                        _isProcessingAccessoryIcons = false;
                        yield break;
                    }
                }

                if (next == null)
                {
                    yield return null;
                    continue;
                }

                var generator = S1MugshotGenerator;
                var mugshotRig = generator != null ? generator.MugshotRig : null;
                var iconGenerator = generator != null ? generator.Generator : null;

                if (mugshotRig == null)
                {
                    Logger.Error("MugshotGenerator or MugshotRig not found. Cannot generate accessory icon.");
                    next.Callback?.Invoke(null);
                    yield return null;
                    continue;
                }

                // Phase 1: Setup without yielding
                Transform mugshotParent = mugshotRig.transform.parent;
                if (mugshotParent != null)
                    mugshotParent.gameObject.SetActive(true);

                // Activate the rig
                mugshotRig.gameObject.SetActive(true);

                // Create minimal AvatarSettings with only the accessory
                var mugshotSettings = CreateMinimalAvatarSettings(next.AccessoryPath, next.AccessoryColor);
                mugshotSettings.Height = 1f;
                mugshotRig.LoadAvatarSettings(mugshotSettings);

                // Set layer for icon generation
                SetLayerRecursively(mugshotRig.gameObject, LayerMask.NameToLayer("IconGeneration"));

                // Enable updateWhenOffscreen for proper bounds calculation
                var skinnedMeshRenderers = mugshotRig.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var smr in skinnedMeshRenderers)
                {
                    smr.updateWhenOffscreen = true;
                }

                // Store original IconGenerator settings
                int originalSize = iconGenerator != null ? iconGenerator.IconSize : 512;
                if (iconGenerator != null)
                    iconGenerator.IconSize = next.IconSize;

                // Give the rig a frame to update meshes/bounds with the new settings before capture
                yield return new WaitForEndOfFrame();

                bool completed = false;
                Texture2D capturedTexture = null;

                // Trigger capture
                mugshotRig.GetMugshot((Action<Texture2D>)(generatedMugshot =>
                {
                    try
                    {
                        if (generatedMugshot != null)
                        {
                            generatedMugshot.Apply();
                            capturedTexture = generatedMugshot;
                            Logger.Msg($"Generated accessory icon for '{next.AccessoryPath}': {generatedMugshot.width}x{generatedMugshot.height}");
                        }
                        else
                        {
                            Logger.Error($"Failed to generate accessory icon for '{next.AccessoryPath}': texture is null");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to finalize accessory icon: {ex.Message}");
                    }
                    finally
                    {
                        completed = true;
                    }
                }));

                // Phase 2: Wait for completion
                while (!completed)
                    yield return null;

                // Invoke the user's callback
                next.Callback?.Invoke(capturedTexture);

                // Phase 3: Restore state without yielding
                if (iconGenerator != null)
                    iconGenerator.IconSize = originalSize;

                // Restore rig to default state
                if (generator != null && generator.DefaultSettings != null)
                    mugshotRig.LoadAvatarSettings(generator.DefaultSettings);
                mugshotRig.gameObject.SetActive(false);

                // Small delay between jobs to let the mugshot rig fully reset
                yield return new WaitForSeconds(0.05f);
            }
        }

        /// <summary>
        /// Creates AvatarSettings based on the local player's current appearance with the specified accessory added.
        /// </summary>
        private static S1AvatarFramework.AvatarSettings CreateMinimalAvatarSettings(string accessoryPath, Color accessoryColor)
        {
            // Try to get the local player's current avatar settings
            var localPlayer = Entities.Player.Local;
            S1AvatarFramework.AvatarSettings settings;

            if (localPlayer != null && localPlayer.CurrentAvatarSettings != null)
            {
                // Clone the player's current avatar settings
#if (IL2CPPMELON)
                var playerSettings = (localPlayer.CurrentAvatarSettings as Il2CppSystem.Object)?.Cast<BasicAvatarSettings>();
#else
                var playerSettings = localPlayer.CurrentAvatarSettings as BasicAvatarSettings;
#endif
                if (playerSettings != null)
                {
                    // Convert BasicAvatarSettings to AvatarSettings
                    settings = playerSettings.GetAvatarSettings();
                    Logger.Msg($"Using local player's avatar settings for accessory icon");
                }
                else
                {
                    // Fallback to minimal settings if cast fails
                    settings = CreateFallbackAvatarSettings();
                }
            }
            else
            {
                // Fallback to minimal settings if no local player
                settings = CreateFallbackAvatarSettings();
            }

            // Add the accessory to the settings
            settings.AccessorySettings.Add(new S1AvatarFramework.AvatarSettings.AccessorySetting
            {
                path = accessoryPath,
                color = accessoryColor
            });

            return settings;
        }

        /// <summary>
        /// Creates fallback minimal AvatarSettings when player settings are unavailable.
        /// </summary>
        private static S1AvatarFramework.AvatarSettings CreateFallbackAvatarSettings()
        {
            Logger.Msg("Local player not available, using fallback avatar settings for accessory icon");
            var settings = ScriptableObject.CreateInstance<S1AvatarFramework.AvatarSettings>();

            // Set minimal defaults
            settings.SkinColor = new Color32(150, 120, 95, 255);
            settings.Height = 1f;
            settings.Gender = 0.5f;
            settings.Weight = 0.5f;
            settings.EyeBallTint = Color.white;
            settings.PupilDilation = 1f;
            settings.HairPath = string.Empty;
            settings.HairColor = Color.black;

            // Configure eyelids to be open so eyes are visible
            settings.LeftEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
            {
                topLidOpen = 0.5f,
                bottomLidOpen = 0.5f
            };
            settings.RightEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
            {
                topLidOpen = 0.5f,
                bottomLidOpen = 0.5f
            };
            settings.LeftEyeLidColor = new Color32(150, 120, 95, 255);
            settings.RightEyeLidColor = new Color32(150, 120, 95, 255);

            // Configure eyebrows
            settings.EyebrowScale = 1f;
            settings.EyebrowThickness = 1f;
            settings.EyebrowRestingHeight = 0f;
            settings.EyebrowRestingAngle = 0f;

            // Add face layers so it's not completely blank
            settings.FaceLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
            {
                layerPath = Entities.Appearances.FaceLayerFields.Face.Neutral,
                layerTint = Color.white
            });

            settings.FaceLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
            {
                layerPath = Entities.Appearances.FaceLayerFields.Eyes.EyeShadow,
                layerTint = new Color(0f, 0f, 0f, 0.7f)
            });

            // Add a simple t-shirt so it's not nude
            settings.BodyLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
            {
                layerPath = Entities.Appearances.BodyLayerFields.Shirts.TShirt,
                layerTint = Color.white
            });

            return settings;
        }

        /// <summary>
        /// Sets the layer of a GameObject and all its children recursively.
        /// </summary>
        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
#if (IL2CPPMELON || IL2CPPBEPINEX)
            // Il2Cpp: use index-based access
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

        #endregion
    }
}
