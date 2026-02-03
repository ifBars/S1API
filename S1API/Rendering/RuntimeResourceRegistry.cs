#if (IL2CPPMELON || IL2CPPBEPINEX)
using Il2CppInterop.Runtime;
#endif

using System;
using System.Collections.Generic;
using HarmonyLib;
using S1API.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Rendering
{
    /// <summary>
    /// Provides runtime registration of Unity Resources that can be loaded via Resources.Load.
    /// This allows mods to inject custom assets without modifying Unity asset bundles.
    /// </summary>
    public static class RuntimeResourceRegistry
    {
        #region Private Members

        private static readonly Log Logger = new Log("S1API.RuntimeResourceRegistry");

        /// <summary>
        /// Primary asset registry - stores the "default" asset for each path (usually a GameObject).
        /// </summary>
        private static readonly Dictionary<string, Object> _registeredAssets = new Dictionary<string, Object>();

        /// <summary>
        /// Typed asset registry - stores assets by (path, type) for typed lookups.
        /// Key format: "path|TypeFullName"
        /// </summary>
        private static readonly Dictionary<string, Object> _typedAssets = new Dictionary<string, Object>();

        private static bool _isPatched;

        #endregion

        #region Public API

        /// <summary>
        /// Registers an asset with a Resources path.
        /// After registration, the asset can be loaded via Resources.Load using the provided path.
        /// </summary>
        /// <param name="resourcePath">The Resources path (e.g., "MyMod/Accessories/CustomHat").</param>
        /// <param name="asset">The Unity Object to register.</param>
        /// <returns>True if registration was successful.</returns>
        public static bool RegisterAsset(string resourcePath, Object asset)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                Logger.Error("Cannot register asset: resourcePath is null or empty");
                return false;
            }

            if (asset == null)
            {
                Logger.Error($"Cannot register asset at '{resourcePath}': asset is null");
                return false;
            }

            EnsurePatched();

            _registeredAssets[resourcePath] = asset;

            string typedKey = GetTypedKey(resourcePath, asset.GetType());
            _typedAssets[typedKey] = asset;

            Logger.Msg($"Registered '{resourcePath}' as type '{asset.GetType().Name}'");
            return true;
        }

        /// <summary>
        /// Registers an asset with a Resources path for a specific type.
        /// This allows a single path to return different assets based on the requested type.
        /// </summary>
        /// <param name="resourcePath">The Resources path.</param>
        /// <param name="asset">The Unity Object to register.</param>
        /// <param name="forType">The type this asset should be returned for.</param>
        /// <returns>True if registration was successful.</returns>
        public static bool RegisterAssetForType(string resourcePath, Object asset, Type forType)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                Logger.Error("Cannot register asset: resourcePath is null or empty");
                return false;
            }

            if (asset == null)
            {
                Logger.Error($"Cannot register asset at '{resourcePath}': asset is null");
                return false;
            }

            if (forType == null)
            {
                Logger.Error($"Cannot register asset at '{resourcePath}': forType is null");
                return false;
            }

            EnsurePatched();

            string typedKey = GetTypedKey(resourcePath, forType);
            _typedAssets[typedKey] = asset;

            Logger.Msg($"Registered '{resourcePath}' for type '{NormalizeTypeName(forType.FullName)}'");
            return true;
        }

        /// <summary>
        /// Registers a GameObject asset with a Resources path.
        /// </summary>
        /// <param name="resourcePath">The Resources path.</param>
        /// <param name="gameObject">The GameObject to register.</param>
        /// <returns>True if registration was successful.</returns>
        public static bool RegisterGameObject(string resourcePath, GameObject gameObject) =>
            RegisterAsset(resourcePath, gameObject);

        /// <summary>
        /// Checks if an asset is registered at the given path.
        /// </summary>
        /// <param name="resourcePath">The Resources path to check.</param>
        /// <returns>True if registered, false otherwise.</returns>
        public static bool IsRegistered(string resourcePath) =>
            _registeredAssets.ContainsKey(resourcePath);

        /// <summary>
        /// Gets a registered asset.
        /// </summary>
        /// <param name="resourcePath">The Resources path.</param>
        /// <returns>The registered asset, or null if not found.</returns>
        public static Object? GetRegisteredAsset(string resourcePath)
        {
            _registeredAssets.TryGetValue(resourcePath, out var asset);
            return asset;
        }

        /// <summary>
        /// Gets a registered asset of a specific type.
        /// </summary>
        /// <typeparam name="T">The type of asset to retrieve.</typeparam>
        /// <param name="resourcePath">The Resources path.</param>
        /// <returns>The registered asset cast to type T, or null if not found or wrong type.</returns>
        public static T? GetRegisteredAsset<T>(string resourcePath) where T : Object
        {
            string typedKey = GetTypedKey(resourcePath, typeof(T));
            if (_typedAssets.TryGetValue(typedKey, out var typedAsset))
                return typedAsset as T;

            if (_registeredAssets.TryGetValue(resourcePath, out var asset))
                return asset as T;

            return null;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets a registered asset for a specific type (non-generic version).
        /// </summary>
        internal static Object? GetRegisteredAssetForType(string resourcePath, Type type)
        {
            string typedKey = GetTypedKey(resourcePath, type);
            if (_typedAssets.TryGetValue(typedKey, out var typedAsset))
                return typedAsset;

            foreach (var kvp in _typedAssets)
            {
                if (kvp.Key.StartsWith(resourcePath + "|") && type.IsInstanceOfType(kvp.Value))
                    return kvp.Value;
            }

            _registeredAssets.TryGetValue(resourcePath, out var asset);
            return asset;
        }

        #endregion

        #region Private Implementation

        /// <summary>
        /// Gets a typed key for the typed asset dictionary.
        /// Normalizes type names by stripping Il2Cpp prefix for consistent lookup.
        /// </summary>
        private static string GetTypedKey(string resourcePath, Type type) =>
            $"{resourcePath}|{NormalizeTypeName(type.FullName)}";

        /// <summary>
        /// Normalizes a type name by stripping IL2CPP prefixes.
        /// On IL2CPP, typeof(SomeClass).FullName returns "Il2CppNamespace.Class"
        /// but the game's Il2CppSystem.Type.FullName returns "Namespace.Class".
        /// </summary>
        private static string NormalizeTypeName(string? typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeName ?? string.Empty;

            // Strip "Il2Cpp" prefix from type names for consistent matching
            // e.g., "Il2CppScheduleOne.AvatarFramework.Accessory" -> "ScheduleOne.AvatarFramework.Accessory"
            if (typeName.StartsWith("Il2Cpp"))
                return typeName.Substring(6);

            return typeName;
        }

        /// <summary>
        /// Ensures the Resources.Load patch is applied.
        /// </summary>
        private static void EnsurePatched()
        {
            if (_isPatched)
                return;

            try
            {
                var harmony = new HarmonyLib.Harmony("S1API.RuntimeResourceRegistry");

                var allLoadMethods = typeof(Resources).GetMethods(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                System.Reflection.MethodInfo? loadWithTypeMethod = null;
                System.Reflection.MethodInfo? loadStringMethod = null;

                foreach (var method in allLoadMethods)
                {
                    if (method.Name != "Load")
                        continue;

                    var parameters = method.GetParameters();

                    // Find Resources.Load(string path, Type systemTypeInstance)
                    // On IL2CPP, the Type parameter is Il2CppSystem.Type, not System.Type
                    if (loadWithTypeMethod == null &&
                        parameters.Length == 2 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType.Name == "Type" &&
                        !method.IsGenericMethod)
                    {
                        loadWithTypeMethod = method;
                    }

                    // Find Resources.Load(string path) - non-generic, returns Object
                    if (loadStringMethod == null &&
                        parameters.Length == 1 &&
                        parameters[0].ParameterType == typeof(string) &&
                        method.ReturnType == typeof(Object) &&
                        !method.IsGenericMethod)
                    {
                        loadStringMethod = method;
                    }
                }

                // Patch Resources.Load(string path, Type systemTypeInstance)
                if (loadWithTypeMethod != null)
                {
                    var prefixMethod = typeof(RuntimeResourceRegistry).GetMethod(
                        nameof(ResourcesLoadPrefix),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                    if (prefixMethod != null)
                        harmony.Patch(loadWithTypeMethod, prefix: new HarmonyMethod(prefixMethod));
                }

                // Patch Resources.Load(string path)
                if (loadStringMethod != null)
                {
                    var prefixMethod = typeof(RuntimeResourceRegistry).GetMethod(
                        nameof(ResourcesLoadStringPrefix),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                    if (prefixMethod != null)
                        harmony.Patch(loadStringMethod, prefix: new HarmonyMethod(prefixMethod));
                }

                _isPatched = true;
                Logger.Msg($"Patched Resources.Load methods (typed={loadWithTypeMethod != null}, string={loadStringMethod != null})");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to patch Resources.Load: {ex.Message}");
                Logger.Error(ex.StackTrace ?? "No stack trace available");
            }
        }

#if (IL2CPPMELON || IL2CPPBEPINEX)
        /// <summary>
        /// Harmony prefix for Resources.Load(string, Il2CppSystem.Type) on IL2CPP.
        /// </summary>
        private static bool ResourcesLoadPrefix(string path, Il2CppSystem.Type systemTypeInstance, ref Object __result)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            string? typeFullName = systemTypeInstance?.FullName;
            string typeName = systemTypeInstance?.Name ?? "null";

            // Try direct lookup in typed registry using normalized type name
            if (!string.IsNullOrEmpty(typeFullName))
            {
                string normalizedTypeName = NormalizeTypeName(typeFullName);
                string typedKey = $"{path}|{normalizedTypeName}";

                if (_typedAssets.TryGetValue(typedKey, out var typedAsset))
                {
                    __result = typedAsset;
                    return false;
                }

                // Check for compatible types by path prefix
                foreach (var kvp in _typedAssets)
                {
                    if (kvp.Key.StartsWith(path + "|"))
                    {
                        __result = kvp.Value;
                        return false;
                    }
                }
            }

            // Check primary registry
            if (_registeredAssets.TryGetValue(path, out var asset))
            {
                if (systemTypeInstance == null)
                {
                    __result = asset;
                    return false;
                }

                // Handle requesting a Component from a GameObject
                if (asset is GameObject gameObject)
                {
                    try
                    {
                        var component = gameObject.GetComponent(systemTypeInstance);
                        if (component != null)
                        {
                            __result = component;
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to get component '{typeName}' from GameObject: {ex.Message}");
                    }

                    Logger.Warning($"Registered GameObject at '{path}' does not have component of type '{typeName}'");
                }
                else
                {
                    __result = asset;
                    return false;
                }
            }

            return true;
        }
#else
        /// <summary>
        /// Harmony prefix for Resources.Load(string, Type) on Mono.
        /// </summary>
        private static bool ResourcesLoadPrefix(string path, Type systemTypeInstance, ref Object __result)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            string typeName = systemTypeInstance?.Name ?? "null";

            // Check typed registry first
            if (systemTypeInstance != null)
            {
                var typedAsset = GetRegisteredAssetForType(path, systemTypeInstance);
                if (typedAsset != null && systemTypeInstance.IsInstanceOfType(typedAsset))
                {
                    __result = typedAsset;
                    return false;
                }
            }

            // Check primary registry
            if (_registeredAssets.TryGetValue(path, out var asset))
            {
                if (systemTypeInstance == null)
                {
                    __result = asset;
                    return false;
                }

                if (systemTypeInstance.IsInstanceOfType(asset))
                {
                    __result = asset;
                    return false;
                }

                // Handle requesting a Component from a GameObject
                if (asset is GameObject gameObject && typeof(Component).IsAssignableFrom(systemTypeInstance))
                {
                    try
                    {
                        var component = gameObject.GetComponent(systemTypeInstance);
                        if (component != null)
                        {
                            __result = component;
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to get component '{typeName}' from GameObject: {ex.Message}");
                    }

                    Logger.Warning($"Registered GameObject at '{path}' does not have component of type '{typeName}'");
                }
            }

            return true;
        }
#endif

        /// <summary>
        /// Harmony prefix for Resources.Load(string) to check our registry first.
        /// </summary>
        private static bool ResourcesLoadStringPrefix(string path, ref Object __result)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            if (_registeredAssets.TryGetValue(path, out var asset))
            {
                __result = asset;
                return false;
            }

            return true;
        }

        #endregion
    }
}
