#if (IL2CPPMELON)
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarFramework = ScheduleOne.AvatarFramework;
#endif

using S1API.Logging;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Rendering
{
    /// <summary>
    /// Factory for creating custom accessory prefabs at runtime by cloning and modifying existing ones.
    /// </summary>
    public static class AccessoryFactory
    {
        private static readonly Log Logger = new Log("AccessoryFactory");
        
        /// <summary>
        /// INTERNAL: Registry of custom textures for accessories.
        /// Maps resource path to texture replacement dictionary.
        /// </summary>
        internal static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, Texture2D>> 
            _accessoryTextureRegistry = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, Texture2D>>();

        /// <summary>
        /// Clones an accessory prefab and applies custom textures/materials.
        /// </summary>
        /// <param name="sourceResourcePath">The Resources path to the source accessory prefab.</param>
        /// <param name="newName">Name for the cloned accessory.</param>
        /// <param name="textureReplacements">Optional dictionary of shader texture names and replacement textures.</param>
        /// <param name="colorTint">Optional color tint to apply.</param>
        /// <param name="targetResourcePath">Optional target resource path for the cloned accessory. If provided, sets Accessory.AssetPath to this value instead of sourceResourcePath. This ensures PlayerClothing validation can match the accessory correctly.</param>
        /// <returns>The cloned and customized accessory GameObject, or null if cloning failed.</returns>
        public static GameObject CloneAccessoryWithCustomTextures(
            string sourceResourcePath,
            string newName,
            System.Collections.Generic.Dictionary<string, Texture2D> textureReplacements = null,
            Color? colorTint = null,
            string targetResourcePath = null)
        {
            try
            {
                // Load the source prefab
                var sourcePrefab = Resources.Load<GameObject>(sourceResourcePath);
                if (sourcePrefab == null)
                {
                    Logger.Error($"Failed to load source accessory at path: {sourceResourcePath}");
                    return null;
                }

                // Clone the prefab
                var clonedPrefab = Object.Instantiate(sourcePrefab);
                clonedPrefab.name = newName;
                Object.DontDestroyOnLoad(clonedPrefab);
                
                // Temporarily set inactive to avoid rendering while we modify it
                clonedPrefab.SetActive(false);

                // Get the Accessory component
                var accessory = clonedPrefab.GetComponent<S1AvatarFramework.Accessory>();
                if (accessory != null)
                {
                    // Set AssetPath to target path if provided, otherwise use source path
                    // This is critical: PlayerClothing.RefreshAppearance validates accessories by comparing
                    // ClothingDefinition.ClothingAssetPath with Accessory.AssetPath. If they don't match,
                    // the accessory gets removed from the avatar settings, making it invisible.
                    accessory.AssetPath = targetResourcePath ?? sourceResourcePath;
                    accessory.Name = newName;
                }

                // Apply custom textures and materials
                if (textureReplacements != null && textureReplacements.Count > 0)
                {
                    ApplyTexturesToAccessory(clonedPrefab, textureReplacements);
                }

                // Apply color tint if specified
                if (colorTint.HasValue && accessory != null)
                {
                    accessory.ApplyColor(colorTint.Value);
                }

                // Set prefab to active so that when Unity instantiates it, the instance will also be active.
                // This is critical: Unity's Object.Instantiate preserves the prefab's active state.
                // If the prefab is inactive, instantiated GameObjects will also be inactive and won't render.
                // Since this prefab is DontDestroyOnLoad and not in any scene hierarchy, it won't render itself.
                clonedPrefab.SetActive(true);

                return clonedPrefab;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to clone accessory '{sourceResourcePath}': {ex.Message}");
                Logger.Error(ex.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Applies custom textures to all materials in an accessory GameObject.
        /// </summary>
        private static void ApplyTexturesToAccessory(
            GameObject accessory,
            System.Collections.Generic.Dictionary<string, Texture2D> textureReplacements)
        {
            // Process all renderers
            var renderers = accessory.GetComponentsInChildren<Renderer>(includeInactive: true);
            foreach (var renderer in renderers)
            {
                // Clone materials to avoid affecting other instances
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = new Material(materials[i]);
                    
                    // Apply texture replacements
                    foreach (var kvp in textureReplacements)
                    {
                        if (material.HasProperty(kvp.Key))
                        {
                            material.SetTexture(kvp.Key, kvp.Value);
                        }
                    }
                    
                    materials[i] = material;
                }
                renderer.materials = materials;
            }
        }

        /// <summary>
        /// Registers a cloned accessory with the RuntimeResourceRegistry so it can be loaded via Resources.Load.
        /// </summary>
        /// <param name="resourcePath">The Resources path to register the accessory at.</param>
        /// <param name="accessory">The accessory GameObject to register.</param>
        /// <returns>True if registration was successful.</returns>
        public static bool RegisterAccessory(string resourcePath, GameObject accessory)
        {
            if (accessory == null)
            {
                Logger.Error("Cannot register null accessory");
                return false;
            }

            // Verify it has an Accessory component
            var accessoryComponent = accessory.GetComponent<S1AvatarFramework.Accessory>();
            if (accessoryComponent == null)
            {
                Logger.Warning($"GameObject '{accessory.name}' does not have an Accessory component. It may not work correctly as clothing.");
            }

            return RuntimeResourceRegistry.RegisterGameObject(resourcePath, accessory);
        }

        /// <summary>
        /// Creates a custom accessory by cloning a source and applies custom textures, then registers it.
        /// This is a convenience method that combines cloning, customization, and registration.
        /// </summary>
        /// <param name="sourceResourcePath">The Resources path to the source accessory prefab.</param>
        /// <param name="targetResourcePath">The Resources path where the custom accessory will be registered.</param>
        /// <param name="newName">Name for the cloned accessory.</param>
        /// <param name="textureReplacements">Optional dictionary of shader texture names and replacement textures.</param>
        /// <param name="colorTint">Optional color tint to apply.</param>
        /// <returns>True if the accessory was successfully created and registered.</returns>
        public static bool CreateAndRegisterAccessory(
            string sourceResourcePath,
            string targetResourcePath,
            string newName,
            System.Collections.Generic.Dictionary<string, Texture2D> textureReplacements = null,
            Color? colorTint = null)
        {
            var clonedAccessory = CloneAccessoryWithCustomTextures(
                sourceResourcePath,
                newName,
                textureReplacements,
                colorTint,
                targetResourcePath);

            if (clonedAccessory == null)
                return false;

            // Register textures for runtime replacement if provided
            if (textureReplacements != null && textureReplacements.Count > 0)
            {
                _accessoryTextureRegistry[targetResourcePath] = textureReplacements;
            }

            return RegisterAccessory(targetResourcePath, clonedAccessory);
        }
        
        /// <summary>
        /// INTERNAL: Gets the texture replacements for a registered accessory path.
        /// </summary>
        internal static System.Collections.Generic.Dictionary<string, Texture2D> GetTextureReplacements(string resourcePath)
        {
            _accessoryTextureRegistry.TryGetValue(resourcePath, out var replacements);
            return replacements;
        }
    }
}

