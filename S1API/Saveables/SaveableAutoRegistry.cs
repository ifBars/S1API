using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using S1API.Internal.Abstraction;
using S1API.Internal.Utils;

namespace S1API.Saveables
{
    /// <summary>
    /// Automatically discovers and manages saveable classes that inherit from Saveable.
    /// </summary>
    internal static class SaveableAutoRegistry
    {
        private static readonly List<Type> _discoveredSaveableTypes = new List<Type>();
        private static readonly Dictionary<Type, Saveable> _instances = new Dictionary<Type, Saveable>();
        private static bool _discoveryPerformed = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets all registered saveable instances, performing discovery if needed.
        /// </summary>
        public static IEnumerable<Saveable> GetRegisteredSaveables()
        {
            lock (_lock)
            {
                if (!_discoveryPerformed)
                {
                    DiscoverSaveableTypes();
                    _discoveryPerformed = true;
                }

                return GetOrCreateInstances();
            }
        }

        /// <summary>
        /// Discovers all classes that inherit from Saveable but are not part of S1API.
        /// </summary>
        /// <remarks>
        /// Goes through <see cref="ReflectionUtils.GetDerivedClasses{TBaseClass}"/>, which only enumerates assemblies
        /// that reference S1API. A name-prefix filter is not enough on IL2CPP: Assembly-CSharp and
        /// Assembly-CSharp-firstpass match no prefix, and Assembly.GetTypes() on an interop assembly can end the
        /// process with an uncatchable ExecutionEngineException (0x80131506) on the first save load.
        /// </remarks>
        private static void DiscoverSaveableTypes()
        {
            try
            {
                // Skip S1API's own types to avoid registering internal classes like NPC
                Assembly s1ApiAssembly = typeof(Saveable).Assembly;

                foreach (Type type in ReflectionUtils.GetDerivedClasses<Saveable>())
                {
                    try
                    {
                        if (type == null || type.Assembly == s1ApiAssembly
                            || type.IsInterface || type.IsGenericTypeDefinition)
                            continue;

                        // Check if this type directly inherits from Saveable
                        if (IsDirectSaveableInheritor(type))
                        {
                            _discoveredSaveableTypes.Add(type);
                        }
                    }
                    catch
                    {
                        // Skip types that can't be analyzed (e.g., generic types with constraints)
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail startup
                try
                {
                    MelonLoader.MelonLogger.Warning($"[S1API] Error during saveable type discovery: {ex.Message}");
                }
                catch
                {
                    // Even logging failed - continue silently
                }
            }
        }

        /// <summary>
        /// Checks if a type directly inherits from Saveable (not through another S1API class).
        /// </summary>
        private static bool IsDirectSaveableInheritor(Type type)
        {
            if (!typeof(Saveable).IsAssignableFrom(type))
                return false;

            if (type.IsAbstract || type.IsInterface)
                return false;

            // Check if the base type is exactly Saveable (not a subclass)
            Type? baseType = type.BaseType;
            return baseType == typeof(Saveable);
        }

        /// <summary>
        /// Gets or creates instances of discovered saveable types.
        /// </summary>
        private static IEnumerable<Saveable> GetOrCreateInstances()
        {
            foreach (Type saveableType in _discoveredSaveableTypes)
            {
                if (!_instances.TryGetValue(saveableType, out Saveable? instance))
                {
                    try
                    {
                        // Try to create instance using parameterless constructor
                        instance = Activator.CreateInstance(saveableType, true) as Saveable;
                        if (instance != null)
                        {
                            _instances[saveableType] = instance;
                        }
                    }
                    catch
                    {
                        // Skip types that can't be instantiated
                    }
                }

                if (instance != null)
                    yield return instance;
            }
        }

        /// <summary>
        /// Clears the discovery cache. Useful for testing or dynamic assembly loading scenarios.
        /// </summary>
        internal static void ClearCache()
        {
            lock (_lock)
            {
                _discoveredSaveableTypes.Clear();
                _instances.Clear();
                _discoveryPerformed = false;
            }
        }
    }
}
