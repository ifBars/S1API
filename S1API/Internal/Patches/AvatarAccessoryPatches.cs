#if (IL2CPPMELON)
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarFramework = ScheduleOne.AvatarFramework;
#endif

using HarmonyLib;
using S1API.Internal.Utils;
using S1API.Logging;
using S1API.Rendering;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Harmony patches for Avatar accessory system.
    /// Applies custom textures to accessories when they are instantiated.
    /// </summary>
    [HarmonyPatch]
    internal static class AvatarAccessoryPatches
    {
        private static readonly Log Logger = new Log("AvatarAccessoryPatches");

        /// <summary>
        /// Patches Avatar.ApplyAccessorySettings to apply custom textures to registered accessories.
        /// </summary>
        [HarmonyPatch]
        internal static class AvatarApplyAccessorySettingsPatch
        {
            static MethodBase? TargetMethod()
            {
                return typeof(S1AvatarFramework.Avatar).GetMethod("ApplyAccessorySettings",
                    BindingFlags.Public | BindingFlags.Instance);
            }

            static void Postfix(S1AvatarFramework.Avatar __instance)
            {
                try
                {
                    // Get the applied accessories array using reflection
                    // On IL2CPP, fields are exposed as properties, so use TryGetFieldOrProperty
                    var appliedAccessories = ReflectionUtils.TryGetFieldOrProperty(__instance, "appliedAccessories");
                    if (appliedAccessories == null) return;

                    // Use reflection to iterate through the array
                    var arrayType = appliedAccessories.GetType();
                    if (!arrayType.IsArray) return;

                    int length = ((Array)appliedAccessories).Length;

                    for (int i = 0; i < length; i++)
                    {
                        string? assetPath = null;

                        try
                        {
                            var accessory = ((Array)appliedAccessories).GetValue(i);
                            if (accessory == null) continue;

                            // Get the AssetPath to check if this is a custom accessory
                            // On IL2CPP, fields are exposed as properties, so use TryGetFieldOrProperty
                            assetPath = ReflectionUtils.TryGetFieldOrProperty(accessory, "AssetPath") as string;
                            if (string.IsNullOrEmpty(assetPath)) continue;

                            // Check if we have texture replacements for this accessory
                            var textureReplacements = AccessoryFactory.GetTextureReplacements(assetPath);
                            if (textureReplacements == null || textureReplacements.Count == 0) continue;

                            // Get the GameObject - accessory is a Component (MonoBehaviour)
                            // Cast to Component to access gameObject reliably on IL2CPP
                            GameObject? accessoryObj = null;
                            if (accessory is Component component)
                            {
                                accessoryObj = component.gameObject;
                            }

                            if (accessoryObj == null) continue;

                            // Apply custom textures to all renderers
                            ApplyTexturesToAccessoryInstance(accessoryObj, textureReplacements);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning($"Error applying custom textures to accessory '{assetPath ?? "<unknown>"}': {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Error applying custom textures to accessories: {ex.Message}");
                }
            }

            private static void ApplyTexturesToAccessoryInstance(
                GameObject accessoryObj,
                Dictionary<string, Texture2D> textureReplacements)
            {
                if (accessoryObj == null || textureReplacements == null || textureReplacements.Count == 0)
                    return;

                // Apply texture to all renderers (including inactive ones)
                var renderers = accessoryObj.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer == null) continue;

                    var materials = renderer.materials;
                    bool materialsChanged = false;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] == null) continue;

                        // Clone material to avoid affecting other instances
                        Material newMaterial = new Material(materials[i]);

                        // Apply texture replacements
                        foreach (var kvp in textureReplacements)
                        {
                            if (newMaterial.HasProperty(kvp.Key))
                            {
                                newMaterial.SetTexture(kvp.Key, kvp.Value);
                                materialsChanged = true;
                            }
                        }

                        if (materialsChanged)
                        {
                            materials[i] = newMaterial;
                        }
                    }

                    if (materialsChanged)
                    {
                        renderer.materials = materials;
                    }
                }
            }
        }
    }
}

