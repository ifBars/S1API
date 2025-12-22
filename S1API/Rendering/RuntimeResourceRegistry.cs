using System;
using System.Collections.Generic;
using System.Reflection;
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
        private static readonly Log Logger = new Log("S1API.RuntimeResourceRegistry");
        private static readonly Dictionary<string, Object> _registeredAssets = new Dictionary<string, Object>();
        private static bool _isPatched = false;

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

            if (_registeredAssets.ContainsKey(resourcePath))
            {
                Logger.Warning($"Asset at path '{resourcePath}' is already registered. Overwriting with new asset.");
            }

            EnsurePatched();
            _registeredAssets[resourcePath] = asset;
            return true;
        }

        /// <summary>
        /// Registers a GameObject asset with a Resources path.
        /// </summary>
        /// <param name="resourcePath">The Resources path.</param>
        /// <param name="gameObject">The GameObject to register.</param>
        /// <returns>True if registration was successful.</returns>
        public static bool RegisterGameObject(string resourcePath, GameObject gameObject)
        {
            return RegisterAsset(resourcePath, gameObject);
        }

        /// <summary>
        /// Checks if an asset is registered at the given path.
        /// </summary>
        /// <param name="resourcePath">The Resources path to check.</param>
        /// <returns>True if registered, false otherwise.</returns>
        public static bool IsRegistered(string resourcePath)
        {
            return _registeredAssets.ContainsKey(resourcePath);
        }

        /// <summary>
        /// Gets a registered asset.
        /// </summary>
        /// <param name="resourcePath">The Resources path.</param>
        /// <returns>The registered asset, or null if not found.</returns>
        public static Object GetRegisteredAsset(string resourcePath)
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
        public static T GetRegisteredAsset<T>(string resourcePath) where T : Object
        {
            if (_registeredAssets.TryGetValue(resourcePath, out var asset))
            {
                return asset as T;
            }
            return null;
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
                var harmony = new HarmonyLib.Harmony("S1API.RuntimeResourceRegistry");
                
                // Get all Resources.Load methods and filter manually to avoid ambiguity
                var allLoadMethods = typeof(Resources).GetMethods(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                System.Reflection.MethodInfo loadWithTypeMethod = null;
                System.Reflection.MethodInfo loadStringMethod = null;
                System.Reflection.MethodInfo loadGenericMethod = null;
                
                foreach (var method in allLoadMethods)
                {
                    if (method.Name != "Load")
                        continue;
                    
                    var parameters = method.GetParameters();
                    
                    // Find Resources.Load(string path, Type systemTypeInstance)
                    if (loadWithTypeMethod == null &&
                        parameters.Length == 2 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType == typeof(Type) &&
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
                    
                    // Find Resources.Load<T>(string path) - generic version
                    if (loadGenericMethod == null &&
                        method.IsGenericMethod &&
                        parameters.Length == 1 &&
                        parameters[0].ParameterType == typeof(string) &&
                        method.GetGenericArguments().Length == 1)
                    {
                        loadGenericMethod = method;
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
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to patch Resources.Load: {ex.Message}");
                Logger.Error(ex.StackTrace);
            }
        }

        /// <summary>
        /// INTERNAL: Harmony prefix for Resources.Load(string, Type) to check our registry first.
        /// </summary>
        private static bool ResourcesLoadPrefix(string path, Type systemTypeInstance, ref Object __result)
        {
            // Handle null/empty paths - let Unity handle these
            if (string.IsNullOrEmpty(path))
            {
                return true; // Continue with original method
            }

            // Check if we have a registered asset for this path
            if (_registeredAssets.TryGetValue(path, out var asset))
            {
                // If no type specified, return the asset as-is
                if (systemTypeInstance == null)
                {
                    __result = asset;
                    return false; // Skip original method
                }

                // Direct type match - return the asset
                if (systemTypeInstance.IsInstanceOfType(asset))
                {
                    __result = asset;
                    return false; // Skip original method
                }

                // Special handling: requesting a Component from a GameObject
                // This is common for typed loads like Resources.Load<Accessory>(path)
                if (asset is GameObject gameObject && typeof(Component).IsAssignableFrom(systemTypeInstance))
                {
                    Component component = null;
                    
                    try
                    {
                        // Use reflection to call GetComponent<T>() with the runtime type
                        // This works in both Mono and IL2CPP by calling the generic method
                        var methods = typeof(GameObject).GetMethods(BindingFlags.Public | BindingFlags.Instance);
                        MethodInfo getComponentGeneric = null;
                        
                        // Find the generic GetComponent<T>() method
                        foreach (var method in methods)
                        {
                            if (method.Name == "GetComponent" && method.IsGenericMethod && method.GetParameters().Length == 0)
                            {
                                getComponentGeneric = method;
                                break;
                            }
                        }
                        
                        if (getComponentGeneric != null)
                        {
                            // Make it generic with the requested type and invoke
                            var genericGetComponent = getComponentGeneric.MakeGenericMethod(systemTypeInstance);
                            component = genericGetComponent.Invoke(gameObject, null) as Component;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to get component '{systemTypeInstance.Name}' from GameObject: {ex.Message}");
                    }
                    
                    if (component != null)
                    {
                        __result = component;
                        return false; // Skip original method
                    }
                    
                    // Component not found on GameObject - let Unity handle it (will return null)
                    Logger.Warning($"Registered GameObject at '{path}' does not have component of type '{systemTypeInstance.Name}'");
                }
            }
            
            return true; // Continue with original method
        }

        /// <summary>
        /// INTERNAL: Harmony prefix for Resources.Load(string) to check our registry first.
        /// </summary>
        private static bool ResourcesLoadStringPrefix(string path, ref Object __result)
        {
            // Handle null/empty paths - let Unity handle these
            if (string.IsNullOrEmpty(path))
            {
                return true; // Continue with original method
            }

            // Check if we have a registered asset for this path
            if (_registeredAssets.TryGetValue(path, out var asset))
            {
                __result = asset;
                return false; // Skip original method
            }
            return true; // Continue with original method
        }


    }
}

