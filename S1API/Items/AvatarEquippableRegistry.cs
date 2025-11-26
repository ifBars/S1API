#if (IL2CPPMELON)
using S1AvatarEquipping = Il2CppScheduleOne.AvatarFramework.Equipping;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarEquipping = ScheduleOne.AvatarFramework.Equipping;
#endif

using System.Collections.Generic;
using HarmonyLib;
using S1API.AssetBundles;
using S1API.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Items
{
    /// <summary>
    /// Manages registration and loading of AvatarEquippable prefabs from AssetBundles.
    /// Allows modders to load AvatarEquippable prefabs and register them so they can be used with equippable items.
    /// </summary>
    public static class AvatarEquippableRegistry
    {
        private static readonly Log _logger = new Log("AvatarEquippableRegistry");
        private static readonly Dictionary<string, GameObject> _registeredPrefabs = new Dictionary<string, GameObject>();
        private static bool _isPatched = false;

        /// <summary>
        /// Registers an AvatarEquippable prefab with a Resources path.
        /// After registration, the prefab can be loaded via Resources.Load using the provided assetPath.
        /// </summary>
        /// <param name="assetPath">The Resources path to register (e.g., "Equippables/MyItem").</param>
        /// <param name="prefab">The AvatarEquippable prefab GameObject to register.</param>
        /// <returns>True if registration was successful.</returns>
        /// <remarks>
        /// The prefab must have an AvatarEquippable component attached.
        /// Ensure the assetPath matches what you'll use in WithAvatarEquippable().
        /// </remarks>
        public static bool RegisterAvatarEquippable(string assetPath, GameObject prefab)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                _logger.Error("Cannot register AvatarEquippable: assetPath is null or empty");
                return false;
            }

            if (prefab == null)
            {
                _logger.Error($"Cannot register AvatarEquippable at '{assetPath}': prefab is null");
                return false;
            }

            // Verify the prefab has an AvatarEquippable component
            var avatarEquippable = prefab.GetComponent<S1AvatarEquipping.AvatarEquippable>();
            if (avatarEquippable == null)
            {
                _logger.Warning($"Prefab '{prefab.name}' does not have an AvatarEquippable component. It will still be registered, but may not work correctly.");
            }

            if (_registeredPrefabs.ContainsKey(assetPath))
            {
                _logger.Warning($"AvatarEquippable at path '{assetPath}' is already registered. Overwriting with new prefab.");
            }

            EnsurePatched();
            _registeredPrefabs[assetPath] = prefab;
            _logger.Msg($"Registered AvatarEquippable prefab: {assetPath}");
            return true;
        }

        /// <summary>
        /// Loads an AvatarEquippable prefab from an AssetBundle and registers it.
        /// </summary>
        /// <param name="bundle">The AssetBundle containing the prefab.</param>
        /// <param name="prefabName">The name of the prefab asset in the bundle.</param>
        /// <param name="assetPath">The Resources path to register (e.g., "Equippables/MyItem").</param>
        /// <returns>True if loading and registration were successful.</returns>
        public static bool LoadAndRegisterFromBundle(WrappedAssetBundle bundle, string prefabName, string assetPath)
        {
            if (bundle == null)
            {
                _logger.Error("Cannot load AvatarEquippable: bundle is null");
                return false;
            }

            try
            {
                var prefab = bundle.LoadAsset<GameObject>(prefabName);
                if (prefab == null)
                {
                    _logger.Error($"Failed to load prefab '{prefabName}' from AssetBundle");
                    return false;
                }

                return RegisterAvatarEquippable(assetPath, prefab);
            }
            catch (System.Exception ex)
            {
                _logger.Error($"Exception loading AvatarEquippable from bundle: {ex.Message}");
                _logger.Error(ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Loads an AvatarEquippable prefab from an embedded AssetBundle and registers it.
        /// </summary>
        /// <param name="bundleName">The name of the embedded AssetBundle resource.</param>
        /// <param name="prefabName">The name of the prefab asset in the bundle.</param>
        /// <param name="assetPath">The Resources path to register (e.g., "Equippables/MyItem").</param>
        /// <param name="assemblyOverride">Optional assembly to load the bundle from. If null, uses executing assembly.</param>
        /// <returns>True if loading and registration were successful.</returns>
        public static bool LoadAndRegisterFromEmbeddedBundle(string bundleName, string prefabName, string assetPath, System.Reflection.Assembly assemblyOverride = null)
        {
            try
            {
                var assembly = assemblyOverride ?? System.Reflection.Assembly.GetExecutingAssembly();
                var bundle = AssetLoader.GetAssetBundleFromStream($"{assembly.GetName().Name}.{bundleName}", assembly);
                return LoadAndRegisterFromBundle(bundle, prefabName, assetPath);
            }
            catch (System.Exception ex)
            {
                _logger.Error($"Exception loading AvatarEquippable from embedded bundle '{bundleName}': {ex.Message}");
                _logger.Error(ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Checks if an AvatarEquippable is registered at the given path.
        /// </summary>
        /// <param name="assetPath">The Resources path to check.</param>
        /// <returns>True if registered, false otherwise.</returns>
        public static bool IsRegistered(string assetPath)
        {
            return _registeredPrefabs.ContainsKey(assetPath);
        }

        /// <summary>
        /// Gets a registered AvatarEquippable prefab.
        /// </summary>
        /// <param name="assetPath">The Resources path.</param>
        /// <returns>The registered prefab, or null if not found.</returns>
        public static GameObject GetRegisteredPrefab(string assetPath)
        {
            _registeredPrefabs.TryGetValue(assetPath, out var prefab);
            return prefab;
        }

        /// <summary>
        /// INTERNAL: Ensures the Resources.Load patch is applied.
        /// </summary>
        private static void EnsurePatched()
        {
            if (_isPatched)
                return;

            try
            {
                var harmony = new HarmonyLib.Harmony("S1API.AvatarEquippableRegistry");
                
                // Patch Resources.Load(string, Type) - the main overload
                var loadWithTypeMethod = typeof(Resources).GetMethod("Load", new[] { typeof(string), typeof(System.Type) });
                if (loadWithTypeMethod != null)
                {
                    var prefixMethod = typeof(AvatarEquippableRegistry).GetMethod(nameof(ResourcesLoadPrefix), 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    harmony.Patch(loadWithTypeMethod, prefix: new HarmonyMethod(prefixMethod));
                }

                // Patch Resources.Load(string) - convenience overload
                var loadStringMethod = typeof(Resources).GetMethod("Load", new[] { typeof(string) });
                if (loadStringMethod != null)
                {
                    var prefixMethod = typeof(AvatarEquippableRegistry).GetMethod(nameof(ResourcesLoadStringPrefix), 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    harmony.Patch(loadStringMethod, prefix: new HarmonyMethod(prefixMethod));
                }

                _isPatched = true;
            }
            catch (System.Exception ex)
            {
                _logger.Error($"Failed to patch Resources.Load: {ex.Message}");
                _logger.Error(ex.StackTrace);
            }
        }

        /// <summary>
        /// INTERNAL: Harmony prefix for Resources.Load(string, Type) to check our registry first.
        /// </summary>
        private static bool ResourcesLoadPrefix(string path, System.Type systemTypeInstance, ref Object __result)
        {
            // Only intercept if it's a GameObject request and we have a registered prefab
            if (systemTypeInstance == typeof(GameObject) || systemTypeInstance == null)
            {
                if (_registeredPrefabs.TryGetValue(path, out var prefab))
                {
                    __result = prefab;
                    return false; // Skip original method
                }
            }
            return true; // Continue with original method
        }

        /// <summary>
        /// INTERNAL: Harmony prefix for Resources.Load(string) to check our registry first.
        /// </summary>
        private static bool ResourcesLoadStringPrefix(string path, ref Object __result)
        {
            // Check if we have a registered prefab for this path
            if (_registeredPrefabs.TryGetValue(path, out var prefab))
            {
                __result = prefab;
                return false; // Skip original method
            }
            return true; // Continue with original method
        }
    }
}

