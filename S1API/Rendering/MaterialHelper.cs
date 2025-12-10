using System;
using UnityEngine;
using UnityEngine.Rendering;
using S1API.Logging;

namespace S1API.Rendering
{
    /// <summary>
    /// Utility methods for working with Unity materials and shaders.
    /// Provides safe, convenient methods for common material operations.
    /// </summary>
    public static class MaterialHelper
    {
        private static readonly Log Logger = new Log("MaterialHelper");

        /// <summary>
        /// Replaces materials on a GameObject and all its children based on a predicate.
        /// Creates new material instances to avoid modifying shared materials.
        /// </summary>
        /// <param name="gameObject">The GameObject to process (will include all children).</param>
        /// <param name="predicate">Function that returns true for materials to replace.</param>
        /// <param name="materialModifier">Action to modify the matched materials.</param>
        /// <example>
        /// <code>
        /// // Replace all materials with "wood" in the name
        /// MaterialHelper.ReplaceMaterials(
        ///     myObject,
        ///     mat => mat.name.ToLower().Contains("wood"),
        ///     mat => {
        ///         MaterialHelper.SetColor(mat, "_BaseColor", Color.red);
        ///         MaterialHelper.SetFloat(mat, "_Metallic", 0.8f);
        ///     }
        /// );
        /// </code>
        /// </example>
        public static void ReplaceMaterials(
            GameObject gameObject,
            Func<Material, bool> predicate,
            Action<Material> materialModifier)
        {
            if (gameObject == null)
            {
                Logger.Warning("ReplaceMaterials called with null GameObject");
                return;
            }

            if (predicate == null || materialModifier == null)
            {
                Logger.Warning("ReplaceMaterials called with null predicate or modifier");
                return;
            }

            try
            {
                // Process all renderers including inactive ones
                foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>(includeInactive: true))
                {
                    if (renderer == null)
                        continue;

                    ProcessRenderer(renderer, predicate, materialModifier);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in ReplaceMaterials: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a metallic material variant from an existing material.
        /// Removes all textures and applies metallic properties.
        /// </summary>
        /// <param name="baseMaterial">The material to clone and modify.</param>
        /// <param name="metalColor">The color to apply to the metal.</param>
        /// <param name="metallic">Metallic value (0.0 to 1.0, default 0.8).</param>
        /// <param name="smoothness">Smoothness value (0.0 to 1.0, default 0.5).</param>
        /// <returns>A new material with metallic properties.</returns>
        /// <example>
        /// <code>
        /// var metalMaterial = MaterialHelper.CreateMetallicVariant(
        ///     originalMaterial,
        ///     new Color(0.5f, 0.5f, 0.55f), // Gray metal
        ///     metallic: 0.8f,
        ///     smoothness: 0.5f
        /// );
        /// </code>
        /// </example>
        public static Material CreateMetallicVariant(
            Material baseMaterial,
            Color metalColor,
            float metallic = 0.8f,
            float smoothness = 0.5f)
        {
            if (baseMaterial == null)
            {
                Logger.Error("CreateMetallicVariant called with null baseMaterial");
                return null;
            }

            try
            {
                // Create a new material instance
                var metalMaterial = new Material(baseMaterial)
                {
                    name = $"{baseMaterial.name}_metallic"
                };

                // Remove all textures
                RemoveAllTextures(metalMaterial);

                // Apply metallic properties
                SetColor(metalMaterial, "_BaseColor", metalColor);
                SetColor(metalMaterial, "_Color", metalColor);
                SetFloat(metalMaterial, "_Metallic", Mathf.Clamp01(metallic));
                SetFloat(metalMaterial, "_Smoothness", Mathf.Clamp01(smoothness));
                SetFloat(metalMaterial, "_Glossiness", Mathf.Clamp01(smoothness));

                return metalMaterial;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating metallic variant: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Removes all textures from a material.
        /// Iterates through all shader properties and sets texture properties to null.
        /// </summary>
        /// <param name="material">The material to modify.</param>
        public static void RemoveAllTextures(Material material)
        {
            if (material == null)
                return;

            try
            {
                var shader = material.shader;
                int propertyCount = shader.GetPropertyCount();

                for (int i = 0; i < propertyCount; i++)
                {
                    if (shader.GetPropertyType(i) == ShaderPropertyType.Texture)
                    {
                        string propertyName = shader.GetPropertyName(i);
                        material.SetTexture(propertyName, null);
                    }
                }

                // Also clear main texture
                material.mainTexture = null;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error removing textures: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets a color property on a material, checking if the property exists first.
        /// </summary>
        /// <param name="material">The material to modify.</param>
        /// <param name="propertyName">The shader property name (e.g., "_BaseColor").</param>
        /// <param name="color">The color value to set.</param>
        /// <returns>True if the property was set, false if it doesn't exist.</returns>
        public static bool SetColor(Material material, string propertyName, Color color)
        {
            if (material == null || string.IsNullOrEmpty(propertyName))
                return false;

            try
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetColor(propertyName, color);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error setting color property '{propertyName}': {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Sets a float property on a material, checking if the property exists first.
        /// </summary>
        /// <param name="material">The material to modify.</param>
        /// <param name="propertyName">The shader property name (e.g., "_Metallic").</param>
        /// <param name="value">The float value to set.</param>
        /// <returns>True if the property was set, false if it doesn't exist.</returns>
        public static bool SetFloat(Material material, string propertyName, float value)
        {
            if (material == null || string.IsNullOrEmpty(propertyName))
                return false;

            try
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetFloat(propertyName, value);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error setting float property '{propertyName}': {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Sets a texture property on a material, checking if the property exists first.
        /// </summary>
        /// <param name="material">The material to modify.</param>
        /// <param name="propertyName">The shader property name (e.g., "_MainTex").</param>
        /// <param name="texture">The texture to set (null to clear).</param>
        /// <returns>True if the property was set, false if it doesn't exist.</returns>
        public static bool SetTexture(Material material, string propertyName, Texture texture)
        {
            if (material == null || string.IsNullOrEmpty(propertyName))
                return false;

            try
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetTexture(propertyName, texture);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error setting texture property '{propertyName}': {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Sets multiple shader properties at once using a configuration action.
        /// </summary>
        /// <param name="material">The material to modify.</param>
        /// <param name="configurator">Action to configure material properties.</param>
        /// <example>
        /// <code>
        /// MaterialHelper.ConfigureMaterial(material, mat => {
        ///     MaterialHelper.SetColor(mat, "_BaseColor", Color.red);
        ///     MaterialHelper.SetFloat(mat, "_Metallic", 0.8f);
        ///     MaterialHelper.SetFloat(mat, "_Smoothness", 0.5f);
        /// });
        /// </code>
        /// </example>
        public static void ConfigureMaterial(Material material, Action<Material> configurator)
        {
            if (material == null || configurator == null)
                return;

            try
            {
                configurator(material);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error configuring material: {ex.Message}");
            }
        }

        private static void ProcessRenderer(Renderer renderer, Func<Material, bool> predicate, Action<Material> materialModifier)
        {
            var materials = renderer.materials; // Creates a copy of the materials array
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                if (mat == null)
                    continue;

                if (predicate(mat))
                {
                    // Create a new instance to avoid modifying shared materials
                    var newMat = new Material(mat)
                    {
                        name = $"{mat.name}_modified"
                    };

                    materialModifier(newMat);
                    materials[i] = newMat;
                    changed = true;
                }
            }

            // Only reassign if we made changes
            if (changed)
            {
                renderer.materials = materials;
            }
        }
    }
}
