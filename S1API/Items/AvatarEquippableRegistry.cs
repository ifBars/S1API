#if (IL2CPPMELON)
using S1AvatarEquipping = Il2CppScheduleOne.AvatarFramework.Equipping;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarEquipping = ScheduleOne.AvatarFramework.Equipping;
#endif

using S1API.AssetBundles;
using S1API.Logging;
using S1API.Rendering;
using UnityEngine;

namespace S1API.Items
{
    /// <summary>
    /// Manages registration and loading of AvatarEquippable prefabs from AssetBundles.
    /// Allows modders to load AvatarEquippable prefabs and register them so they can be used with equippable items.
    /// </summary>
    /// <remarks>
    /// This class now uses RuntimeResourceRegistry internally for asset registration.
    /// </remarks>
    public static class AvatarEquippableRegistry
    {
        private static readonly Log _logger = new Log("AvatarEquippableRegistry");

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

            // Use RuntimeResourceRegistry for actual registration
            bool success = RuntimeResourceRegistry.RegisterGameObject(assetPath, prefab);
            if (success)
            {
                _logger.Msg($"Registered AvatarEquippable prefab: {assetPath}");
            }
            return success;
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
            return RuntimeResourceRegistry.IsRegistered(assetPath);
        }

        /// <summary>
        /// Gets a registered AvatarEquippable prefab.
        /// </summary>
        /// <param name="assetPath">The Resources path.</param>
        /// <returns>The registered prefab, or null if not found.</returns>
        public static GameObject GetRegisteredPrefab(string assetPath)
        {
            return RuntimeResourceRegistry.GetRegisteredAsset<GameObject>(assetPath);
        }
    }
}

